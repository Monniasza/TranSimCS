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
    public sealed class NodeSpec: IEquatable<NodeSpec>, IEnumerable<LaneNode> {
        public readonly ImmutableArray<LaneNode> Lanes;
        public readonly ImmutableDictionary<Guid, LaneNode> LaneXRef;
        public readonly Range<float> Range;
        public static readonly NodeSpec Empty = new([]);
        public NodeSpec(IEnumerable<LaneNode> data) {
            Range = data.Select(x => x.Bounds).AggregateOrDefault(new(0, 0), (x, y) => x.Union(y));
            Lanes = data.OrderBy(x => x.Bounds.Middle()).ToImmutableArray();
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

        public IEnumerator<LaneNode> GetEnumerator() => ((IEnumerable<LaneNode>)Lanes).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }
}
