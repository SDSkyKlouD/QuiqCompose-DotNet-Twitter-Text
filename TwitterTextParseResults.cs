namespace SDSK.QuiqCompose.Library.TwitterText {
    public class TwitterTextParseResults {
        public int WeightedLength { get; set; }
        public int Permillage { get; set; }
        public bool IsValid { get; set; }
        public Range DisplayTextRange { get; set; }
        public Range ValidTextRange { get; set; }

        public override int GetHashCode() {
            int result = WeightedLength;
            result = 31 * result + Permillage;
            result = 31 * result + IsValid.GetHashCode();
            result = 31 * result + DisplayTextRange.GetHashCode();
            result = 31 * result + ValidTextRange.GetHashCode();
            return result;
        }

        public override bool Equals(object obj)
            => this == obj || obj is TwitterTextParseResults && Equals((TwitterTextParseResults) obj);

        private bool Equals(TwitterTextParseResults obj)
            => obj != null && obj.WeightedLength == WeightedLength
            && obj.Permillage == Permillage && obj.IsValid == IsValid
            && obj.DisplayTextRange.Equals(DisplayTextRange)
            && obj.ValidTextRange.Equals(ValidTextRange);
    }
}
