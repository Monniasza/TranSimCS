using System;

namespace TranSimCS.Roads.Strip {
    public enum SegmentHalf {
        Start, // Represents the left half of a road segment
        End // Represents the right half of a road segment
    }
    public static class SegmentHalfMethods {
        public static int Discriminant(this SegmentHalf half) => half.GetConditionalOrInvalid(-1, 1, 0);
        public static T GetConditional<T>(this SegmentHalf half, T start, T end) => half switch {
            SegmentHalf.Start => start,
            SegmentHalf.End => end,
            _ => throw new ArgumentException("Invalid segment half specified."),
        };
        public static T GetConditionalOrInvalid<T>(this SegmentHalf half, T start, T end, T invalid) => half switch {
            SegmentHalf.Start => start,
            SegmentHalf.End => end,
            _ => invalid,
        };

        public static SegmentHalf Inverse(this SegmentHalf half) => half switch {
            SegmentHalf.End => SegmentHalf.Start,
            SegmentHalf.Start => SegmentHalf.End,
            _ => half
        };
    }
}
