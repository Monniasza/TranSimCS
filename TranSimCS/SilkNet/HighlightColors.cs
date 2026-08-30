using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Decides which colors are used for highlighting. There are 2 colors: one for the selected subelement and other for the entire object
    /// </summary>
    public struct HighlightColors : IEquatable<HighlightColors> {
        public Color ObjectColor;
        public Color ComponentColor;

        public HighlightColors(Color objectColor, Color componentColor) {
            ObjectColor = objectColor;
            ComponentColor = componentColor;
        }

        public override bool Equals(object? obj) {
            return obj is HighlightColors colors && Equals(colors);
        }

        public bool Equals(HighlightColors other) {
            return ObjectColor.Equals(other.ObjectColor) &&
                   ComponentColor.Equals(other.ComponentColor);
        }

        public override int GetHashCode() {
            return HashCode.Combine(ObjectColor, ComponentColor);
        }

        public static bool operator ==(HighlightColors left, HighlightColors right) {
            return left.Equals(right);
        }

        public static bool operator !=(HighlightColors left, HighlightColors right) {
            return !(left == right);
        }

        public static HighlightColors DefaultHighlightColor => new(Colors.SemiClearAzure, Colors.Yellow);

        public static HighlightColors DemolitionHighlightColor => new(Colors.SemiClearRed, Colors.Orange);
    }
}
