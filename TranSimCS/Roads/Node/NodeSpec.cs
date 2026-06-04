using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Geometry;

namespace TranSimCS.Roads.Node {
    public sealed class LaneNode(LaneSpec laneSpec, float centerPos, Guid? guid = null): IComparable<LaneNode>, IEquatable<LaneNode>{
        public readonly Guid ID = guid ?? Guid.NewGuid();
        public readonly LaneSpec LaneSpec = laneSpec;
        public readonly float CenterPos = centerPos;

        public LaneNode WithBounds(Range<float> bounds) {
            var laneSpec = LaneSpec;
            var centerPos = (bounds.Min + bounds.Max) /2;
            laneSpec.Width = bounds.Max - bounds.Min;
            return new LaneNode(laneSpec, centerPos);
        }

        public static LaneNode FromBounds(LaneSpec spec, Range<float> bounds, Guid? guid = null){
            spec.Width = bounds.Max - bounds.Min;
            var cpos = (bounds.Min + bounds.Max) / 2;
            return new(spec, cpos, guid);
        }

        public int CompareTo(LaneNode? other) {
            if(other == null) return 1;
            if(this == other) return 0;
            var compareCenters = CenterPos.CompareTo(other.CenterPos);
            if(compareCenters != 0) return compareCenters;
            var compareWidths = LaneSpec.Width.CompareTo(other.LaneSpec.Width);
            if(compareWidths != 0) return compareWidths;
            return ID.CompareTo(other.ID);
        }

        public bool Equals(LaneNode? other) {
            return other != null && this.ID == other.ID && this.LaneSpec == other.LaneSpec && this.CenterPos == other.CenterPos;
        }

        public Range<float> Bounds {
            get => new(CenterPos - LaneSpec.Width / 2, CenterPos + LaneSpec.Width / 2);
        }

        public static bool operator <(LaneNode left, LaneNode right) {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(LaneNode left, LaneNode right) {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(LaneNode left, LaneNode right) {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(LaneNode left, LaneNode right) {
            return left.CompareTo(right) >= 0;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (ReferenceEquals(obj, null)) {
                return false;
            }

            return Equals((LaneNode)obj);
        }

        public override int GetHashCode() {
            return HashCode.Combine(CenterPos, LaneSpec, ID);
        }

        public LaneNode WithGUID(Guid iD) {
            return new(LaneSpec, CenterPos, iD);
        }
        public LaneNode WithSpec(LaneSpec spec) {
            return new(spec, CenterPos, ID);
        }

        public static bool operator ==(LaneNode left, LaneNode right) {
            if (ReferenceEquals(left, null)) {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(LaneNode left, LaneNode right) {
            return !(left == right);
        }
    }
    public sealed class NodeSpec: IEquatable<NodeSpec>, IEnumerable<LaneNode> {
        public readonly IList<LaneNode> Lanes;
        public readonly IDictionary<Guid, LaneNode> LaneXRef;
        public readonly Range<float> Range;
        public static readonly NodeSpec Empty = new([]);
        public NodeSpec(IEnumerable<LaneNode> data) {
            Range = data.Select(x => x.Bounds).AggregateOrDefault(new(0, 0), (x, y) => x.Union(y));
            Lanes = data.Order().ToImmutableList();
            LaneXRef = data.Select(x => new KeyValuePair<Guid, LaneNode>(x.ID, x)).ToImmutableDictionary();
        }

        public bool Equals(NodeSpec? other) {
            if(other == null) return false;
            if(this == other) return true;
            //Cross-match the cross-references
            if(this.LaneXRef.Count != other.LaneXRef.Count) return false;
            foreach (var kvp in this.LaneXRef) {
                if (other.LaneXRef.TryGetValue(kvp.Key, out var value)) {
                    //Lanes must match
                    var thisValue = kvp.Value;
                    if(thisValue.LaneSpec != value.LaneSpec) return false;
                    if(thisValue.CenterPos != value.CenterPos) return false;
                } else 
                    //If the lane does not have a corresponding lane from the other node, it is definitely not equal
                    return false;
            }
            return true;
        }

        public IEnumerator<LaneNode> GetEnumerator() => Lanes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }
}
