using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class TwitterTextConfiguration {
        private const int DEFAULT_VERSION = 2;
        private const int DEFAULT_WEIGHTED_LENGTH = 280;
        private const int DEFAULT_SCALE = 100;
        private const int DEFAULT_WEIGHT = 200;
        private const int DEFAULT_TRANSFORMED_URL_LENGTH = 23;
        private static readonly List<TwitterTextWeightedRange> DEFAULT_RANGES = new List<TwitterTextWeightedRange>();

        static TwitterTextConfiguration() {
            DEFAULT_RANGES.Add(new TwitterTextWeightedRange() {
                Start = 0,
                End = 4351,
                Weight = 100
            });
            DEFAULT_RANGES.Add(new TwitterTextWeightedRange() {
                Start = 8192,
                End = 8205,
                Weight = 100
            });
            DEFAULT_RANGES.Add(new TwitterTextWeightedRange() {
                Start = 8208,
                End = 8223,
                Weight = 100
            });
            DEFAULT_RANGES.Add(new TwitterTextWeightedRange() {
                Start = 8232,
                End = 8233,
                Weight = 100
            });
            DEFAULT_RANGES.Add(new TwitterTextWeightedRange() {
                Start = 8242,
                End = 8247,
                Weight = 100
            });
        }

        public ConfigurationProperties Properties { get; set; }

        public static TwitterTextConfiguration ConfigurationFromJson(string json, bool isResource) {
            TwitterTextConfiguration config = new TwitterTextConfiguration();

            try {
                if(isResource) {
                    string jsonRaw = GetEmbeddedResource("Resources/" + json, Assembly.GetExecutingAssembly());
                    ConfigurationProperties jsonDeserialized = JsonConvert.DeserializeObject<ConfigurationProperties>(jsonRaw);
                    config.Properties = jsonDeserialized;
                } else {
                    config.Properties = JsonConvert.DeserializeObject<ConfigurationProperties>(json);
                }
            } catch {
                return GetDefaultConfig();
            }

            return config;
        }

        private static TwitterTextConfiguration GetDefaultConfig()
            => new TwitterTextConfiguration {
                Properties = new ConfigurationProperties() {
                    Version = DEFAULT_VERSION,
                    MaxWeightedTweetLength = DEFAULT_WEIGHTED_LENGTH,
                    Scale = DEFAULT_SCALE,
                    DefaultWeight = DEFAULT_WEIGHT,
                    Ranges = DEFAULT_RANGES,
                    TransformedUrlLength = DEFAULT_TRANSFORMED_URL_LENGTH
                }
            };

        public override int GetHashCode() {
            int result = 17;
            result = result * 31 + Properties.Version;
            result = result * 31 + Properties.MaxWeightedTweetLength;
            result = result * 31 + Properties.Scale;
            result = result * 31 + Properties.DefaultWeight;
            result = result * 31 + Properties.TransformedUrlLength;
            result = result * 31 + Properties.Ranges.GetHashCode();
            return result;
        }

        public override bool Equals(object obj) {
            if(this == obj) {
                return true;
            }

            if(obj == null || GetType() != obj.GetType()) {
                return false;
            }

            TwitterTextConfiguration that = (TwitterTextConfiguration) obj;
            return Properties.Version == that.Properties.Version
                && Properties.MaxWeightedTweetLength == that.Properties.MaxWeightedTweetLength
                && Properties.Scale == that.Properties.Scale
                && Properties.DefaultWeight == that.Properties.DefaultWeight
                && Properties.TransformedUrlLength == that.Properties.TransformedUrlLength
                && Properties.Ranges.Equals(that.Properties.Ranges);
        }

        private static string GetEmbeddedResource(string resName, Assembly assembly) {
            resName = FormatResourceName(assembly, resName);

            using(Stream resStream = assembly.GetManifestResourceStream(resName)) {
                if(resStream == null) {
                    return null;
                }

                using(StreamReader reader = new StreamReader(resStream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        private static string FormatResourceName(Assembly assembly, string resName)
            => assembly.GetName().Name + "." + resName.Replace(' ', '_')
                                                      .Replace('\\', '.')
                                                      .Replace('/', '.');
    }

    public class ConfigurationProperties {
        public int Version { get; set; }
        public int MaxWeightedTweetLength { get; set; }
        public int Scale { get; set; }
        public int DefaultWeight { get; set; }
        public int TransformedUrlLength { get; set; }
        public List<TwitterTextWeightedRange> Ranges { get; set; }
    }

    public class TwitterTextWeightedRange {
        public int Start { get; set; }
        public int End { get; set; }
        public int Weight { get; set; }

        public Range getRange()
            => new Range(Start, End);

        public override int GetHashCode()
            => 31 * Start + 31 * End + 31 * Weight;

        public override bool Equals(object obj) {
            if(this == obj) {
                return true;
            }

            if(obj == null || GetType() != obj.GetType()) {
                return false;
            }

            TwitterTextWeightedRange that = (TwitterTextWeightedRange) obj;
            return (Start == that.Start) && (End == that.End) && (Weight == that.Weight);
        }
    }
}
