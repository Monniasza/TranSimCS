using System;
using MonoGame.Extended;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Menus.InGame {
    public struct AddLaneSelection: IRoadElement {
        public sbyte side; //-1 for left, 1 for right
        public float position;
        public RoadNodeEnd nodeEnd;

        public Guid Guid => throw new NotImplementedException();

        public AddLaneSelection(sbyte side, float position, RoadNodeEnd nodeEnd) {
            this.side = side;
            this.position = position;
            this.nodeEnd = nodeEnd;
        }

        public Range<float> CalculateOffsets(float width) {
            if(side < 0) 
                return new(position - width, position);
            return new(position, position + width);
        }
        public float CalculateOffset(float width) {
            if(side < 0) return position - width;
            return position + width;
        }
        /// <summary>
        /// Creates the new lane for this add-lane button. This becomes invalid after addition for new lane creations.
        /// The newly created lane is already added to the node
        /// </summary>
        /// <param name="spec">lane spec to use</param>
        /// <returns>a new lane</returns>
        public LaneEnd NewLane(LaneSpec spec) {
            var positions = CalculateOffsets(spec.Width);
            LaneNode laneNode = LaneNode.FromBounds(spec, positions);
            Lane newLane = nodeEnd.Node.AddLane(laneNode);
            return newLane.GetEnd(nodeEnd.End);
        }

        public int ZDiscriminant() => nodeEnd.ZDiscriminant();
        public int XDiscriminant() => side;
        public LaneStrip? GetLaneStrip() => null;
        public RoadStrip? GetRoadStrip() => null;
        public RoadNode GetRoadNode() => nodeEnd.Node;
        public Lane? GetLane() => null;
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd GetNodeEnd() => nodeEnd;
        int? IRoadElement.GetIndexInHalfNode() => (side * ZDiscriminant() > 0) ? nodeEnd.Node.Lanes.Count : -1;
    }
}
