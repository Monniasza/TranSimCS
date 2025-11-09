namespace TranSimCS.Roads {
    public enum SegmentHalf {
        Start, // Represents the left half of a road segment
        End // Represents the right half of a road segment
    }
    public static class SegmentHalfMethods {
        public static int Discriminant(this SegmentHalf half) {
            if (half == SegmentHalf.Start) return -1;
            if (half == SegmentHalf.End) return 1;
            return 0;
        }
    }
}
