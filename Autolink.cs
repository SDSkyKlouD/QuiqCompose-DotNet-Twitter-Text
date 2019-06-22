using System.Collections.Generic;
using System.Text;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class Autolink {
        public static readonly string DEFAULT_LIST_CLASS = "tweet-url list-slug";
        public static readonly string DEFAULT_USERNAME_CLASS = "tweet-url username";
        public static readonly string DEFAULT_HASHTAG_CLASS = "tweet-url hashtag";
        public static readonly string DEFAULT_CASHTAG_CLASS = "tweet-url cashtag";
        public static readonly string DEFAULT_USERNAME_URL_BASE = "https://twitter.com/";
        public static readonly string DEFAULT_LIST_URL_BASE = "https://twitter.com/";
        public static readonly string DEFAULT_HASHTAG_URL_BASE = "https://twitter.com/search?q=%23";
        public static readonly string DEFAULT_CASHTAG_URL_BASE = "https://twitter.com/search?q=%24";
        public static readonly string DEFAULT_INVISIBLE_TAG_ATTRS = "style='position:absolute;left:-9999px;'";

        protected string UrlClass { get; set; } = null;
        protected string ListClass { get; set; }
        protected string UsernameClass { get; set; }
        protected string HashtagClass { get; set; }
        protected string CashtagClass { get; set; }
        protected string UsernameUrlBase { get; set; }
        protected string ListUrlBase { get; set; }
        protected string HashtagUrlBase { get; set; }
        protected string CashtagUrlBase { get; set; }
        protected string InvisibleTagAttrs { get; set; }
        protected bool NoFollow { get; set; } = true;
        protected bool UsernameIncludeSymbol { get; set; } = false;
        protected string SymbolTag { get; set; } = null;
        protected string TextWithSymbolTag { get; set; } = null;
        protected string UrlTarget { get; set; } = null;
        protected ILinkAttributeModifier LinkAttributeModifier { get; set; } = null;
        protected ILinkTextModifier LinkTextModifier { get; set; } = null;

        private Extractor _extractor = new Extractor();

        private static string EscapeHtml(string text) {
            StringBuilder builder = new StringBuilder(text.Length * 2);

            for(int i = 0; i < text.Length; i++) {
                char c = text[i];

                switch(c) {
                    case '&':
                        builder.Append("&amp;");
                        break;
                    case '>':
                        builder.Append("&gt;");
                        break;
                    case '<':
                        builder.Append("&lt;");
                        break;
                    case '"':
                        builder.Append("&quot;");
                        break;
                    case '\'':
                        builder.Append("&#39;");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        public Autolink()
            : this(true) { }

        public Autolink(bool noFollow) {
            UrlClass = null;
            ListClass = DEFAULT_LIST_CLASS;
            UsernameClass = DEFAULT_USERNAME_CLASS;
            HashtagClass = DEFAULT_HASHTAG_CLASS;
            CashtagClass = DEFAULT_CASHTAG_CLASS;
            UsernameUrlBase = DEFAULT_USERNAME_URL_BASE;
            ListUrlBase = DEFAULT_LIST_URL_BASE;
            HashtagUrlBase = DEFAULT_HASHTAG_URL_BASE;
            CashtagUrlBase = DEFAULT_CASHTAG_URL_BASE;
            InvisibleTagAttrs = DEFAULT_INVISIBLE_TAG_ATTRS;

            _extractor.ExtractUrlWithoutProtocol = false;
            NoFollow = noFollow;
        }

        public string EscapeBrackets(string text) {
            int len = text.Length;
            StringBuilder sb = new StringBuilder(len + 16);

            if(len == 0) {
                return text;
            }

            for(int i = 0; i < len; ++i) {
                char c = text[i];
                switch(c) {
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public void LinkToText(Entity entity, string originalText, IDictionary<string, string> attributes,
                               StringBuilder builder) {
            if(NoFollow) {
                attributes.Add("rel", "nofollow");
            }

            if(LinkAttributeModifier != null) {
                LinkAttributeModifier.Modify(entity, attributes);
            }

            string text = originalText;

            if(LinkTextModifier != null) {
                text = LinkTextModifier.Modify(entity, originalText);
            }

            builder.Append("<a");

            foreach(var entry in attributes) {
                builder.Append(" ").Append(EscapeHtml(entry.Key)).Append("=\"").
                    Append(EscapeHtml(entry.Value)).Append("\"");
            }

            builder.Append(">").Append(text).Append("</a>");
        }

        public void LinkToTextWithSymbol(Entity entity, string symbol, string originalText,
                                         IDictionary<string, string> attributes, StringBuilder builder) {
            string taggedSymbol = SymbolTag == null || SymbolTag.Length == 0 ? symbol : string.Format("<{0}>{1}</{0}>", SymbolTag, symbol);
            string text = EscapeHtml(originalText);
            string taggedText = TextWithSymbolTag == null || TextWithSymbolTag.Length == 0 ? text : string.Format("<{0}>{1}</{0}>", TextWithSymbolTag, text, TextWithSymbolTag);

            bool includeSymbol = UsernameIncludeSymbol || !Regex.AT_SIGNS.IsMatch(symbol);

            if(includeSymbol) {
                LinkToText(entity, taggedSymbol + taggedText, attributes, builder);
            } else {
                builder.Append(taggedSymbol);
                LinkToText(entity, taggedText, attributes, builder);
            }
        }

        public void LinkToHashtag(Entity entity, string text, StringBuilder builder) {
            string hashChar = text.Substring(entity.Start, 1);
            string hashTag = entity.Value;

            IDictionary<string, string> attrs = new Dictionary<string, string>();
            attrs.Add("href", HashtagUrlBase + hashTag);
            attrs.Add("title", "#" + hashTag);

            if(Regex.RTL_CHARACTERS.IsMatch(text)) {
                attrs.Add("class", HashtagClass + " rtl");
            } else {
                attrs.Add("class", HashtagClass);
            }

            LinkToTextWithSymbol(entity, hashChar, hashTag, attrs, builder);
        }

        public void LinkToCashtag(Entity entity, string text, StringBuilder builder) {
            string cashTag = entity.Value;

            IDictionary<string, string> attrs = new Dictionary<string, string>();
            attrs.Add("href", CashtagUrlBase + cashTag);
            attrs.Add("title", "$" + cashTag);
            attrs.Add("class", CashtagClass);

            LinkToTextWithSymbol(entity, "$", cashTag, attrs, builder);
        }

        public void LinkToMentionAndList(Entity entity, string text, StringBuilder builder) {
            string mention = entity.Value;
            string atChar = text.Substring(entity.Start, 1);

            IDictionary<string, string> attrs = new Dictionary<string, string>();

            if(entity.ListSlug != null) {
                mention += entity.ListSlug;
                attrs.Add("class", ListClass);
                attrs.Add("href", ListUrlBase + mention);
            } else {
                attrs.Add("class", UsernameClass);
                attrs.Add("href", UsernameUrlBase + mention);
            }

            LinkToTextWithSymbol(entity, atChar, mention, attrs, builder);
        }

        public void LinkToUrl(Entity entity, string text, StringBuilder builder) {
            string url = entity.Value;
            string linkText = EscapeHtml(url);

            if(entity.DisplayUrl != null && entity.ExpandedUrl != null) {
                string displayUrlSansEllipses = entity.DisplayUrl.Replace("…", "");
                int displayUrlIndexInExpandedUrl = entity.ExpandedUrl.IndexOf(displayUrlSansEllipses);

                if(displayUrlIndexInExpandedUrl != -1) {
                    string beforeDisplayUrl = entity.ExpandedUrl.Substring(0, displayUrlIndexInExpandedUrl);
                    string afterDisplayUrl = entity.ExpandedUrl.Substring(displayUrlIndexInExpandedUrl + displayUrlSansEllipses.Length);
                    string precedingEllipsis = entity.DisplayUrl.StartsWith("…") ? "…" : "";
                    string followingEllipsis = entity.DisplayUrl.EndsWith("…") ? "…" : "";
                    string invisibleSpan = "<span " + InvisibleTagAttrs + ">";

                    StringBuilder sb = new StringBuilder("<span lass='tco-ellipsis'>");
                    sb.Append(precedingEllipsis);
                    sb.Append(invisibleSpan).Append("&nbsp;</span></span>");
                    sb.Append(invisibleSpan).Append(EscapeHtml(beforeDisplayUrl)).Append("</span>");
                    sb.Append("<span class='js-display-url'>").Append(EscapeHtml(displayUrlSansEllipses)).Append("</span>");
                    sb.Append(invisibleSpan).Append(EscapeHtml(afterDisplayUrl)).Append("</span>");
                    sb.Append("<span class='tco-ellipsis'>").Append(invisibleSpan).Append("&nbsp;</span>").Append(followingEllipsis).Append("</span>");

                    linkText = sb.ToString();
                } else {
                    linkText = entity.DisplayUrl;
                }
            }

            IDictionary<string, string> attrs = new Dictionary<string, string>();
            attrs.Add("href", url);

            if(UrlClass != null) {
                attrs.Add("class", UrlClass);
            }
            if(!string.IsNullOrEmpty(UrlClass)) {
                attrs.Add("class", UrlClass);
            }
            if(!string.IsNullOrEmpty(UrlTarget)) {
                attrs.Add("target", UrlTarget);
            }

            LinkToText(entity, linkText, attrs, builder);
        }

        public string AutoLinkEntities(string text, List<Entity> entities) {
            StringBuilder builder = new StringBuilder(text.Length * 2);
            int beginIndex = 0;

            foreach(var entity in entities) {
                builder.Append(text.Substring(beginIndex, entity.Start - beginIndex));

                switch(entity.EntityType) {
                    case Entity.Type.URL:
                        LinkToUrl(entity, text, builder);
                        break;
                    case Entity.Type.HASHTAG:
                        LinkToHashtag(entity, text, builder);
                        break;
                    case Entity.Type.MENTION:
                        LinkToMentionAndList(entity, text, builder);
                        break;
                    case Entity.Type.CASHTAG:
                        LinkToCashtag(entity, text, builder);
                        break;
                    default:
                        break;
                }

                beginIndex = entity.End;
            }

            builder.Append(text.Substring(beginIndex, text.Length - beginIndex));

            return builder.ToString();
        }

        public string AutoLink(string originalText) {
            string text = EscapeBrackets(originalText);
            List<Entity> entities = _extractor.ExtractEntitiesWithIndices(text);
            return AutoLinkEntities(text, entities);
        }

        public string AutoLinkUsernamesAndLists(string text)
            => AutoLinkEntities(text, _extractor.ExtractMentionsOrListsWithIndices(text));

        public string AutoLinkHashtags(string text)
            => AutoLinkEntities(text, _extractor.ExtractHashtagsWithIndices(text));

        public string AutoLinkUrls(string text)
            => AutoLinkEntities(text, _extractor.ExtractUrlsWithIndices(text));
    }

    public interface ILinkAttributeModifier {
        void Modify(Entity entitiy, IDictionary<string, string> attributes);
    }

    public interface ILinkTextModifier {
        string Modify(Entity entity, string text);
    }
}
