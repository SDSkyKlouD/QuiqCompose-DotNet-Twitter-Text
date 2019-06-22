using System.Collections.Generic;
using System.Text.RegularExpressions;
using Pattern = System.Text.RegularExpressions.Regex;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class Regex {
        #region private constants
        private static readonly string URL_VALID_GTLD =
            "(?:(?:" + Join(TldLists.GTLDS) + ")(?=[^a-z0-9@]|$))";
        private static readonly string URL_VALID_CCTLD =
            "(?:(?:" + Join(TldLists.CTLDS) + ")(?=[^a-z0-9@]|$))";

        private static readonly string INVALID_CHARACTERS =
            "\uFFFE" + "\uFEFF" +       // BOM
            "\uFFFF" +                  // Special
            "\u202A-\u202E";            // Direction change

        private static readonly string UNICODE_SPACES = "[" +
            "\u0009-\u000D" +           // # White_Space # Cc
            "\u0020" +                  // White_Space # Zs     Space
            "\u0085" +                  // White_Space # Cc
            "\u00A0" +                  // White_Space # Zs     No-break Space
            "\u1680" +                  // White_Space # Zs     Ogham Space Mark
            "\u180E" +                  // White_Space # Zs     Mongolian Vowel Separator
            "\u2000-\u200A" +           // # White_Space # Zs   EN Quad ... Hair Space
            "\u2028" +                  // White_Space # Zl     Line Separator
            "\u2029" +                  // White_Space # Zp     Paragraph Separator
            "\u202F" +                  // White_Space # Zs     Narrow No-break Space 
            "\u205F" +                  // White_Space # Zs     Medium Mathematical Space
            "\u3000" +                  // White_Space # Zs     Ideographic Space
        "]";

        private static readonly string LATIN_ACCENTS_CHARS =
            // Latin-1
            "\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u00FF" +
            // Latin Extended A and B
            "\u0100-\u024F" +
            // IPA Extensions
            "\u0253\u0254\u0256\u0257\u0259\u025B\u0263\u0268\u026F\u0272\u0289\u028B" +
            // Hawaiian
            "\u02BB" +
            // Combining Diacritics
            "\u0300-\u036F" +
            // Latin Extended Additional (for Vietnamese)
            "\u1E00-\u1EFF";

        private static readonly string CYRILLIC_CHARS = "\u0400-\u04FF";

        private static readonly string HASHTAG_LETTERS_AND_MARKS = "\\p{L}\\p{M}";

        private static readonly string HASHTAG_NUMERALS = "\\p{Nd}";

        private static readonly string HASHTAG_SPECIAL_CHARS = "_" +
            "\\u200c" +         // ZERO WIDTH NON-JOINER (ZWNJ)
            "\\u200d" +         // ZERO WIDTH JOINER (ZWJ)
            "\\ua67e" +         // CYRILLIC KAVYKA
            "\\u05be" +         // HEBREW PUNCTUATION MAQAF
            "\\u05f3" +         // HEBREW PUNCTUATION GERESH
            "\\u05f4" +         // HEBREW PUNCTUATION GERSHAYIM
            "\\uff5e" +         // FULLWIDTH TILDE
            "\\u301c" +         // WAVE DASH
            "\\u309b" +         // KATAKANA-HIRAGANA VOICED SOUND MARK
            "\\u309c" +         // KATAKANA-HIRAGANA SEMI-VOICED SOUND MARK
            "\\u30a0" +         // KATAKANA-HIRAGANA DOUBLE HYPHEN
            "\\u30fb" +         // KATAKANA MIDDLE DOT
            "\\u3003" +         // DITTO MARK
            "\\u0f0b" +         // TIBETAN MARK INTERSYLLABIC TSHEG
            "\\u0f0c" +         // TIBETAN MARK DELIMITER TSHEG BSTAR
            "\\u00b7";          // MIDDLE DOT

        private static readonly string HASHTAG_LETTERS_NUMERALS = HASHTAG_LETTERS_AND_MARKS + HASHTAG_NUMERALS + HASHTAG_SPECIAL_CHARS;
        private static readonly string HASHTAG_LETTERS_SET = "[" + HASHTAG_LETTERS_AND_MARKS + "]";
        private static readonly string HASHTAG_LETTERS_NUMERALS_SET = "[" + HASHTAG_LETTERS_NUMERALS + "]";

        private static readonly string URL_VALID_PRECEEDING_CHARS = "(?:[^a-z0-9@＠$#＃" + INVALID_CHARACTERS + "]|^)";
        private static readonly string URL_VALID_CHARS = "a-z0-9" + LATIN_ACCENTS_CHARS + "";
        private static readonly string URL_VALID_SUBDOMAIN = "(?>(?:[" + URL_VALID_CHARS + "][" + URL_VALID_CHARS + "\\-_]*)?[" + URL_VALID_CHARS + "]\\.)";
        private static readonly string URL_VALID_DOMAIN_NAME = "(?:(?:[" + URL_VALID_CHARS + "][" + URL_VALID_CHARS + "\\-]*)?[" + URL_VALID_CHARS + "]\\.)";

        private static readonly string PUNCTUATION_CHARS = "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~".Replace(@"\", @"\\").Replace(@"]", @"\]").Replace(@"-", @"\-");

        private static readonly string URL_VALID_UNICODE_CHARS = PUNCTUATION_CHARS + "\\s\\p{Z}\\p{IsGeneralPunctuation}";
        private static readonly string URL_VALID_UNICODE_DOMAIN_NAME = "(?:(?:[^" + URL_VALID_UNICODE_CHARS + "][^" + URL_VALID_UNICODE_CHARS + "\\-]*)?[^" + URL_VALID_UNICODE_CHARS + "]\\.)";

        private static readonly string URL_PUNYCODE = "(?:xn--[-0-9a-z]+)";

        private static readonly string URL_VALID_DOMAIN =
            "(?:" +                                                                 // optional sub-domain + domain + TLD
                URL_VALID_SUBDOMAIN + "+" + URL_VALID_DOMAIN_NAME +                 // e.g. twitter.com, foo.co.jp ...
                "(?:" + URL_VALID_GTLD + "|" + URL_VALID_CCTLD + "|" + URL_PUNYCODE + ")" +
            ")" +
            "|(?:" + "(?<=https?://)" +
                "(?:" +
                    "(?:" + URL_VALID_DOMAIN_NAME + URL_VALID_CCTLD + ")" +         // protocol + domain + ccTLD
                        "|(?:" +
                            URL_VALID_UNICODE_DOMAIN_NAME +                         // protocol + unicode domain + TLD
                            "(?:" + URL_VALID_GTLD + "|" + URL_VALID_CCTLD + ")" +
                        ")" +
                    ")" +
                ")" +
            "|(?:" +                                                                // domain + ccTLD + '/'
                URL_VALID_DOMAIN_NAME + URL_VALID_CCTLD + "(?=/)" +                 // e.g. t.co/
            ")";

        private static readonly string URL_VALID_PORT_NUMBER = "(?>[0-9]+)";

        private static readonly string URL_VALID_GENERAL_PATH_CHARS = "[a-z0-9!\\*';:=\\+,.\\$/%#\\[\\]\\-\\u2013_~\\|&@" + LATIN_ACCENTS_CHARS + CYRILLIC_CHARS + "]";

        private static readonly string URL_BALANCED_PARENS =
            "\\(" +
                "(?:" +
                    URL_VALID_GENERAL_PATH_CHARS + "+" +
                    "|" +
                    // allow one nested level of balanced parentheses
                    "(?:" +
                        URL_VALID_GENERAL_PATH_CHARS + "*" +
                        "\\(" +
                            URL_VALID_GENERAL_PATH_CHARS + "+" +
                        "\\)" +
                         URL_VALID_GENERAL_PATH_CHARS + "*" +
                    ")" +
                ")" +
            "\\)";

        private static readonly string URL_VALID_PATH_ENDING_CHARS = "[a-z0-9=_#/\\-\\+" + LATIN_ACCENTS_CHARS + CYRILLIC_CHARS + "]|(?:" + URL_BALANCED_PARENS + ")";

        private static readonly string URL_VALID_PATH =
            "(?:" +
                "(?:" +
                    URL_VALID_GENERAL_PATH_CHARS + "*" +
                    "(?:" + URL_BALANCED_PARENS + URL_VALID_GENERAL_PATH_CHARS + "*)*" +
                    URL_VALID_PATH_ENDING_CHARS +
                ")|(?:@" + URL_VALID_GENERAL_PATH_CHARS + "+/)" +
            ")";

        private static readonly string URL_VALID_URL_QUERY_CHARS = "[a-z0-9!?\\*'\\(\\);:&=\\+\\$/%#\\[\\]\\-_\\.,~\\|@]";
        private static readonly string URL_VALID_URL_QUERY_ENDING_CHARS = "[a-z0-9\\-_&=#/]";
        private static readonly string VALID_URL_PATTERN_STRING =
            "(" +                                                   //  $1 total match
                "(" + URL_VALID_PRECEEDING_CHARS + ")" +            //  $2 Preceeding chracter
                "(" +                                               //  $3 URL
                    "(https?://)?" +                                //  $4 Protocol (optional)
                    "(" + URL_VALID_DOMAIN + ")" +                  //  $5 Domain(s)
                    "(?::(" + URL_VALID_PORT_NUMBER + "))?" +       //  $6 Port number (optional)
                    "(/" +
                        "(?>" + URL_VALID_PATH + "*)" +
                    ")?" +                                          //  $7 URL Path and anchor
                    "(\\?" + URL_VALID_URL_QUERY_CHARS + "*" +      //  $8 Query String 
                        URL_VALID_URL_QUERY_ENDING_CHARS + ")?" +
                ")" +
            ")";

        private static readonly string AT_SIGNS_CHARS = "@\uFF20";
        private static readonly string DOLLAR_SIGN_CHAR = "\\$";
        private static readonly string CASHTAG = "[a-z]{1,6}(?:[._][a-z]{1,2})?";
        #endregion

        #region public constants
        public static readonly Pattern INVALID_CHARACTERS_PATTERN;
        public static readonly Pattern VALID_HASHTAG;
        public static readonly int VALID_HASHTAG_GROUP_BEFORE = 1;
        public static readonly int VALID_HASHTAG_GROUP_HASH = 2;
        public static readonly int VALID_HASHTAG_GROUP_TAG = 3;
        public static readonly Pattern INVALID_HASHTAG_MATCH_END;
        public static readonly Pattern RTL_CHARACTERS;

        public static readonly Pattern AT_SIGNS;
        public static readonly Pattern VALID_MENTION_OR_LIST;
        public static readonly int VALID_MENTION_OR_LIST_GROUP_BEFORE = 1;
        public static readonly int VALID_MENTION_OR_LIST_GROUP_AT = 2;
        public static readonly int VALID_MENTION_OR_LIST_GROUP_USERNAME = 3;
        public static readonly int VALID_MENTION_OR_LIST_GROUP_LIST = 4;

        public static readonly Pattern VALID_REPLY;
        public static readonly int VALID_REPLY_GROUP_USERNAME = 1;

        public static readonly Pattern INVALID_MENTION_MATCH_END;

        public static readonly Pattern VALID_URL;
        public static readonly int VALID_URL_GROUP_ALL = 1;
        public static readonly int VALID_URL_GROUP_BEFORE = 2;
        public static readonly int VALID_URL_GROUP_URL = 3;
        public static readonly int VALID_URL_GROUP_PROTOCOL = 4;
        public static readonly int VALID_URL_GROUP_DOMAIN = 5;
        public static readonly int VALID_URL_GROUP_PORT = 6;
        public static readonly int VALID_URL_GROUP_PATH = 7;
        public static readonly int VALID_URL_GROUP_QUERY_STRING = 8;

        public static readonly Pattern VALID_TCO_URL;
        public static readonly Pattern INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN;

        public static readonly Pattern VALID_CASHTAG;
        public static readonly int VALID_CASHTAG_GROUP_BEFORE = 1;
        public static readonly int VALID_CASHTAG_GROUP_DOLLAR = 2;
        public static readonly int VALID_CASHTAG_GROUP_CASHTAG = 3;

        public static readonly Pattern VALID_DOMAIN;
        #endregion

        #region static constructor (pattern setup)
        static Regex() {
            INVALID_CHARACTERS_PATTERN = new Pattern(".*[" + INVALID_CHARACTERS + "].*");
            VALID_HASHTAG = new Pattern("(^|\uFE0E|\uFE0F|[^&" + HASHTAG_LETTERS_NUMERALS +
                                        "])([#\uFF03])(?![\uFE0F\u20E3])(" + HASHTAG_LETTERS_NUMERALS_SET + "*" +
                                        HASHTAG_LETTERS_SET + HASHTAG_LETTERS_NUMERALS_SET + "*)", RegexOptions.IgnoreCase);
            INVALID_HASHTAG_MATCH_END = new Pattern("^(?:[#＃]|://)");
            RTL_CHARACTERS = new Pattern("[\u0600-\u06FF\u0750-\u077F\u0590-\u05FF\uFE70-\uFEFF]");
            AT_SIGNS = new Pattern("[" + AT_SIGNS_CHARS + "]");
            VALID_MENTION_OR_LIST = new Pattern("([^a-z0-9_!#$%&*" + AT_SIGNS_CHARS +
                                                "]|^|(?:^|[^a-z0-9_+~.-])RT:?)(" + AT_SIGNS +
                                                "+)([a-z0-9_]{1,20})(/[a-z][a-z0-9_\\-]{0,24})?", RegexOptions.IgnoreCase);
            VALID_REPLY = new Pattern("^(?:" + UNICODE_SPACES + ")*" + AT_SIGNS + "([a-z0-9_]{1,20})", RegexOptions.IgnoreCase);
            INVALID_MENTION_MATCH_END = new Pattern("^(?:[" + AT_SIGNS_CHARS + LATIN_ACCENTS_CHARS + "]|://)");
            INVALID_URL_WITHOUT_PROTOCOL_MATCH_BEGIN = new Pattern("[-_./]$");

            VALID_URL = new Pattern(VALID_URL_PATTERN_STRING, RegexOptions.IgnoreCase);
            VALID_TCO_URL = new Pattern("^https?://t\\.co/([a-z0-9]+)", RegexOptions.IgnoreCase);
            VALID_CASHTAG = new Pattern("(^|" + UNICODE_SPACES + ")(" + DOLLAR_SIGN_CHAR + ")(" +
                                        CASHTAG + ")" + "(?=$|\\s|[" + PUNCTUATION_CHARS + "])", RegexOptions.IgnoreCase);
            VALID_DOMAIN = new Pattern(URL_VALID_DOMAIN, RegexOptions.IgnoreCase);
        }
        #endregion

        private static string Join(IEnumerable<string> values)
            => string.Join("|", values);
    }
}
