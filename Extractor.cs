using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class Extractor {
        private class StartIndexComparer : Comparer<Entity> {
            public override int Compare(Entity x, Entity y)
                => x.Start - y.Start;
        }

        public const int MAX_URL_LENGTH = 4096;
        public const int MAX_TCO_SLUG_LENGTH = 40;
        private static readonly int URL_GROUP_PROTOCOL_LENGTH = "https://".Length;
        public bool ExtractUrlWithoutProtocol { get; set; } = false;

        public Extractor() { }

        private void RemoveOverlappingEntities(List<Entity> entities) {
            entities.Sort(new StartIndexComparer());

            List<Entity> toBeRemoved = new List<Entity>();
            if(entities.Count > 0) {
                IEnumerator<Entity> it = entities.GetEnumerator();
                it.MoveNext();
                Entity prev = it.Current;

                while(it.MoveNext()) {
                    Entity cur = it.Current;

                    if(prev.End > cur.Start) {
                        toBeRemoved.Add(cur);
                    } else {
                        prev = cur;
                    }
                }

                foreach(var entity in toBeRemoved) {
                    entities.Remove(entity);
                }
            }
        }

        public List<Entity> ExtractEntitiesWithIndices(string text) {
            List<Entity> entities = new List<Entity>();
            entities.AddRange(ExtractUrlsWithIndices(text));
            entities.AddRange(ExtractHashtagsWithIndices(text, false));
            entities.AddRange(ExtractMentionsOrListsWithIndices(text));
            entities.AddRange(ExtractCashtagsWithIndices(text));

            RemoveOverlappingEntities(entities);
            return entities;
        }

        public List<string> ExtractMentionedScreennames(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<string>();
            }

            List<string> extracted = new List<string>();
            foreach(var entity in ExtractMentionedScreennamesWithIndices(text)) {
                extracted.Add(entity.Value);
            }

            return extracted;
        }

        public List<Entity> ExtractMentionedScreennamesWithIndices(string text) {
            List<Entity> extracted = new List<Entity>();

            foreach(var entity in ExtractMentionsOrListsWithIndices(text)) {
                if(entity.ListSlug == null) {
                    extracted.Add(entity);
                }
            }

            return extracted;
        }

        public List<Entity> ExtractMentionsOrListsWithIndices(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<Entity>();
            }

            /* Performance optimization */
            bool found = false;
            foreach(char c in text.ToCharArray()) {
                if(c == '@' || c == '＠') {
                    found = true;
                    break;
                }
            }
            if(!found) {
                return new List<Entity>();
            }
            /* === */

            List<Entity> extracted = new List<Entity>();
            MatchCollection matcher = Regex.VALID_MENTION_OR_LIST.Matches(text);
            foreach(Match match in matcher) {
                string after = text.Substring(match.Index + match.Length);

                if(!Regex.INVALID_MENTION_MATCH_END.IsMatch(after)) {
                    if(!match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Success) {
                        extracted.Add(new Entity(match, Entity.Type.MENTION, Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME));
                    } else {
                        extracted.Add(new Entity(match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME].Index - 1,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Index + match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Length,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_USERNAME].Value,
                            match.Groups[Regex.VALID_MENTION_OR_LIST_GROUP_LIST].Value,
                            Entity.Type.MENTION));
                    }
                }
            }

            return extracted;
        }

        public string ExtractReplyScreenname(string text) {
            if(string.IsNullOrEmpty(text)) {
                return null;
            }

            Match matcher = Regex.VALID_REPLY.Match(text);
            if(matcher.Success) {
                string after = text.Substring(matcher.Index + matcher.Length);

                if(Regex.INVALID_MENTION_MATCH_END.IsMatch(after)) {
                    return null;
                } else {
                    return matcher.Groups[Regex.VALID_REPLY_GROUP_USERNAME].Value;
                }
            } else {
                return null;
            }
        }

        public List<string> ExtractUrls(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<string>();
            }

            List<string> urls = new List<string>();
            foreach(var entity in ExtractUrlsWithIndices(text)) {
                urls.Add(entity.Value);
            }

            return urls;
        }

        public List<Entity> ExtractUrlsWithIndices(string text) {
            /* Performance optimization */
            if(string.IsNullOrEmpty(text) ||
                (ExtractUrlWithoutProtocol ? text.IndexOf('.') : text.IndexOf(':')) == -1) {
                return new List<Entity>();
            }
            /* === */

            List<Entity> urls = new List<Entity>();

            MatchCollection matcher = Regex.VALID_URL.Matches(text);
            foreach(Match match in matcher) {
                var protocolMatcher = match.Groups[Regex.VALID_URL_GROUP_PROTOCOL];
                if(!protocolMatcher.Success) {
                    if(!ExtractUrlWithoutProtocol
                        || Regex.INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN.IsMatch(match.Groups[Regex.VALID_URL_GROUP_BEFORE].Value)) {
                        continue;
                    }
                }
                
                string url = match.Groups[Regex.VALID_URL_GROUP_URL].Value;
                int start = match.Groups[Regex.VALID_URL_GROUP_URL].Index;
                int end = match.Groups[Regex.VALID_URL_GROUP_URL].Index + match.Groups[Regex.VALID_URL_GROUP_URL].Length;
                
                Match tcoMatcher = Regex.VALID_TCO_URL.Match(url);
                if(tcoMatcher.Success) {
                    string tcoUrl = tcoMatcher.Groups[0].Value;
                    string tcoUrlSlug = tcoMatcher.Groups[1].Value;

                    if(tcoUrlSlug.Length > MAX_TCO_SLUG_LENGTH) {
                        continue;
                    } else {
                        url = tcoUrl;
                        end = start + url.Length;
                    }
                }

                string host = match.Groups[Regex.VALID_URL_GROUP_DOMAIN].Value;
                if(IsValidHostAndLength(url.Length, protocolMatcher.Value, host)) {
                    urls.Add(new Entity(start, end, url, Entity.Type.URL));
                }
            }

            return urls;
        }

        public static bool IsValidHostAndLength(int originalUrlLength, string protocol, string originalHost) {
            if(string.IsNullOrEmpty(originalHost)) {
                return false;
            }

            int originalHostLength = originalHost.Length;
            string host;

            try {
                IdnMapping mapping = new IdnMapping();
                host = mapping.GetAscii(originalHost);
            } catch(ArgumentException) {
                return false;
            }

            int punycodeEncodedHostLength = host.Length;
            if(punycodeEncodedHostLength == 0) {
                return false;
            }

            int urlLength = originalUrlLength + punycodeEncodedHostLength - originalHostLength;
            int urlLengthWithProtocol = urlLength + (protocol == null ? URL_GROUP_PROTOCOL_LENGTH : 0);

            return urlLengthWithProtocol <= MAX_URL_LENGTH;
        }

        public List<string> ExtractHashtags(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<string>();
            }

            List<string> extracted = new List<string>();
            foreach(var entity in ExtractHashtagsWithIndices(text)) {
                extracted.Add(entity.Value);
            }

            return extracted;
        }

        public List<Entity> ExtractHashtagsWithIndices(string text)
            => ExtractHashtagsWithIndices(text, true);

        private List<Entity> ExtractHashtagsWithIndices(string text, Boolean checkUrlOverlap) {
            if(string.IsNullOrEmpty(text)) {
                return new List<Entity>();
            }

            /* Performance optimization */
            bool found = false;
            foreach(char c in text.ToCharArray()) {
                if(c == '#' || c == '＃') {
                    found = true;
                    break;
                }
            }
            if(!found) {
                return new List<Entity>();
            }
            /* === */

            List<Entity> extracted = new List<Entity>();
            MatchCollection matcher = Regex.VALID_HASHTAG.Matches(text);

            foreach(Match match in matcher) {
                string after = text.Substring(match.Index + match.Length);
                if(!Regex.INVALID_HASHTAG_MATCH_END.IsMatch(after)) {
                    extracted.Add(new Entity(match, Entity.Type.HASHTAG, Regex.VALID_HASHTAG_GROUP_TAG));
                }
            }

            if(checkUrlOverlap) {
                List<Entity> urls = ExtractUrlsWithIndices(text);
                if(urls.Count > 0) {
                    extracted.AddRange(urls);
                    RemoveOverlappingEntities(extracted);

                    List<Entity> toBeRemoved = new List<Entity>();
                    IEnumerator<Entity> it = extracted.GetEnumerator();
                    while(it.MoveNext()) {
                        Entity entity = it.Current;
                        if(entity.EntityType != Entity.Type.HASHTAG) {
                            toBeRemoved.Add(entity);
                        }
                    }

                    foreach(var entity in toBeRemoved) {
                        extracted.Remove(entity);
                    }
                }
            }

            return extracted;
        }

        public List<string> ExtractCashtags(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<string>();
            }

            List<string> extracted = new List<string>();
            foreach(var entity in ExtractCashtagsWithIndices(text)) {
                extracted.Add(entity.Value);
            }

            return extracted;
        }

        public List<Entity> ExtractCashtagsWithIndices(string text) {
            if(string.IsNullOrEmpty(text)) {
                return new List<Entity>();
            }

            /* Performance optimization */
            if(text.IndexOf("$") == -1) {
                return new List<Entity>();
            }
            /* === */

            List<Entity> extracted = new List<Entity>();
            MatchCollection matcher = Regex.VALID_CASHTAG.Matches(text);

            foreach(Match match in matcher) {
                extracted.Add(new Entity(match, Entity.Type.CASHTAG, Regex.VALID_CASHTAG_GROUP_CASHTAG));
            }

            return extracted;
        }

        /* public void ModifyIndicesFromUnicodeToUtf16(string text, List<Entity> entities) {
            IndexConverter convert = new IndexConverter(text);

            foreach(var entity in entities) {
                entity.Start = convert.CodePointsToCodeUnits(entity.Start);
                entity.End = convert.CodePointsToCodeUnits(entity.End);
            }
        }

        public void ModifyIndicesFromUtf16ToUnicode(string text, List<Entity> entities) {
            IndexConverter converter = new IndexConverter(text);

            foreach(var entity in entities) {
                entity.Start = convert.CodeUnitsToCodePoints(entity.Start);
                entity.End = convert.CodeUnitsToCodePoints(entity.End);
            }
        } */
    }

    public class Entity {
        public enum Type {
            URL, HASHTAG, MENTION, CASHTAG
        }

        public int Start { get; private set; }
        public int End { get; private set; }
        public string Value { get; private set; }
        public string ListSlug { get; private set; }
        public Type EntityType { get; private set; }

        public string DisplayUrl { get; set; }
        public string ExpandedUrl { get; set; }

        public Entity(int start, int end, string value, string listSlug, Type type) {
            Start = start;
            End = end;
            Value = value;
            ListSlug = listSlug;
            EntityType = type;
        }

        public Entity(int start, int end, string value, Type type)
            : this(start, end, value, null, type) { }

        public Entity(Match matcher, Type type, int groupNumber)
            : this(matcher, type, groupNumber, -1) { }

        public Entity(Match matcher, Type type, int groupNumber, int startOffset)
            : this(matcher.Groups[groupNumber].Index + startOffset,
                  matcher.Groups[groupNumber].Index + matcher.Groups[groupNumber].Length,
                  matcher.Groups[groupNumber].Value, type) { }

        public override bool Equals(object obj) {
            if(this == obj) {
                return true;
            }

            if(!(obj is Entity)) {
                return false;
            }

            Entity other = (Entity) obj;

            return EntityType.Equals(other.EntityType) &&
                Start == other.Start &&
                End == other.End &&
                Value.Equals(other.Value);
        }

        public override int GetHashCode()
            => EntityType.GetHashCode() + Value.GetHashCode() + Start + End;

        public override string ToString()
            => Value + "(" + EntityType + ") [" + Start + ", " + End + "]";
    }
}
