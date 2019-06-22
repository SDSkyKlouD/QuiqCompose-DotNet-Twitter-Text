using System;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class Validator {
        public const int MAX_TWEET_LENGTH = 280;

        public int ShortUrlLength { get; set; } = 23;
        public int ShortUrlLengthHttps { get; set; } = 23;

        private Extractor extractor = new Extractor();

        [Obsolete("Use TwitterTextParser")]
        public int GetTweetLength(string tweetText)
            => TwitterTextParser.ParseTweet(tweetText).WeightedLength;

        [Obsolete("Use TwitterTextParser")]
        public bool IsValidTweet(string text)
            => TwitterTextParser.ParseTweet(text).IsValid;

        public static bool HasInvalidCharacters(string text)
            => Regex.INVALID_CHARACTERS_PATTERN.IsMatch(text);
    }
}
