using System;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class Range : IComparable<Range> {
        public static readonly Range EMPTY = new Range(-1, -1);

        public readonly int start;
        public readonly int end;

        public Range(int start, int end) {
            this.start = start;
            this.end = end;
        }

        public override bool Equals(object obj)
            => (this == obj || obj is Range)
                && (((Range) obj).start == start && ((Range) obj).end == end);

        public override int GetHashCode()
            => 31 * start + 31 * end;

        public int CompareTo(Range other) {
            if(start < other.start) {
                return -1;
            } else if(start == other.start) {
                if(end < other.end) {
                    return -1;
                } else {
                    return end == other.end ? 0 : 1;
                }
            } else {
                return 1;
            }
        }

        public bool IsInRange(int value)
            => value >= start && value <= end;
    }
}
