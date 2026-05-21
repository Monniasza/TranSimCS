namespace TranSimCS.Tools {
    public enum Alignment {
        Left = 0, Center = 1, Right = 2
    }

    public static class AlignmentMethods {
        public static (float l, float r) GetAlignments(this Alignment alignment) {
            int convertedValue = (int)alignment;
            float r = convertedValue * 0.5f;
            return (1 - r, r);
        }

        public static Alignment Inverse(this Alignment alignment) {
            int ordinal = (int)alignment;
            int inverted = 2 - ordinal;
            return (Alignment)inverted;
        }
    }
}
