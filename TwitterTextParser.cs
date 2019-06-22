using System;
using System.Collections.Generic;
using System.Text;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class TwitterTextParser {
        private TwitterTextParser() { }

        public static readonly TwitterTextParseResults EMPTY_TWITTER_TEXT_PARSE_RESULTS =
            new TwitterTextParseResults {
                WeightedLength = 0,
                Permillage = 0,
                IsValid = false,
                DisplayTextRange = Range.EMPTY,
                ValidTextRange = Range.EMPTY
            };

        public static readonly TwitterTextConfiguration TWITTER_TEXT_DEFAULT_CONFIG =
            TwitterTextConfiguration.ConfigurationFromJson("v1.json", true);

        public static readonly TwitterTextConfiguration TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG =
            TwitterTextConfiguration.ConfigurationFromJson("v2.json", true);

        private static readonly Extractor EXTRACTOR = new Extractor();
        
        public static TwitterTextParseResults ParseTweet(string tweet)
            => ParseTweet(tweet, TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG);

        public static TwitterTextParseResults ParseTweet(string tweet, TwitterTextConfiguration config)
            => ParseTweet(tweet, config, true);

        public static TwitterTextParseResults ParseTweetWithoutExtraction(string tweet)
            => ParseTweet(tweet, TWITTER_TEXT_WEIGHTED_CHAR_COUNT_CONFIG, false);

        public static TwitterTextParseResults ParseTweet(string tweet, TwitterTextConfiguration config, bool extractUrls, bool fixCountNewLine = true) {
            if(string.IsNullOrEmpty(tweet.Trim())) {
                return EMPTY_TWITTER_TEXT_PARSE_RESULTS;
            }

            string normalizedTweet = tweet.Normalize(NormalizationForm.FormC);
            normalizedTweet = fixCountNewLine ? normalizedTweet.Replace(Environment.NewLine, "\n") : normalizedTweet;

            int tweetLength = normalizedTweet.Length;

            if(tweetLength == 0) {
                return EMPTY_TWITTER_TEXT_PARSE_RESULTS;
            }

            int scale = config.Properties.Scale;
            int maxWeightedTweetLength = config.Properties.MaxWeightedTweetLength;
            int scaledMaxWeightedTweetLength = maxWeightedTweetLength * scale;
            int transformedUrlWeight = config.Properties.TransformedUrlLength * scale;
            List<TwitterTextWeightedRange> ranges = config.Properties.Ranges;

            List<Entity> urlEntities = EXTRACTOR.ExtractUrlsWithIndices(normalizedTweet);

            bool hasInvalidCharacters = false;
            int weightedCount = 0;
            int offset = 0;
            int validOffset = 0;

            while(offset < tweetLength) {
                int charWeight = config.Properties.DefaultWeight;

                if(extractUrls) {
                    List<Entity> toBeRemoved = new List<Entity>();
                    IEnumerator<Entity> urlEntityIterator = urlEntities.GetEnumerator();
                    while(urlEntityIterator.MoveNext()) {
                        Entity urlEntity = urlEntityIterator.Current;

                        if(urlEntity.Start == offset) {
                            int urlLength = urlEntity.End - urlEntity.Start;
                            weightedCount += transformedUrlWeight;
                            offset += urlLength;

                            if(weightedCount <- scaledMaxWeightedTweetLength) {
                                validOffset += urlLength;
                            }

                            toBeRemoved.Add(urlEntity);
                            break;
                        }
                    }

                    foreach(var entity in toBeRemoved) {
                        urlEntities.Remove(entity);
                    }
                }

                if(offset < tweetLength) {
                    int codePoint = normalizedTweet[offset];
                    
                    foreach(var weightedRange in ranges) {
                        if(weightedRange.getRange().IsInRange(codePoint)) {
                            charWeight = weightedRange.Weight;
                            break;
                        }
                    }

                    weightedCount += charWeight;
                    
                    hasInvalidCharacters = hasInvalidCharacters || Validator.HasInvalidCharacters(normalizedTweet.Substring(offset, 1));

                    int CharCount(int codepoint) => (codepoint >= 0x10000 ? 2 : 1);
                    int charCount = CharCount(codePoint);
                    offset += charCount;

                    if(!hasInvalidCharacters && weightedCount <= scaledMaxWeightedTweetLength) {
                        validOffset += charCount;
                    }
                }
            }

            int normalizedTweetOffset = tweet.Length - normalizedTweet.Length;
            int scaledWeightedLength = weightedCount / scale;
            bool isValid = !hasInvalidCharacters && (scaledWeightedLength <= maxWeightedTweetLength);
            int permillage = scaledWeightedLength * 1000 / maxWeightedTweetLength;

            return new TwitterTextParseResults {
                WeightedLength = scaledWeightedLength,
                Permillage = permillage,
                IsValid = isValid,
                DisplayTextRange = new Range(0, offset + normalizedTweetOffset - 1),
                ValidTextRange = new Range(0, validOffset + normalizedTweetOffset - 1)
            };
        }
    }
}
