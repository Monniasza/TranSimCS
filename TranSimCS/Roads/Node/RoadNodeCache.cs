using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Roads.Node {
    public struct RoadNodeCache {
        public NodeSpec NodeSpec;
        public Transform3 ReferenceFrame;
        public IList<Lane> SortedLanes;
        public Vector3 CenterPosition;

        internal RoadNodeCache(RoadNode node) {
            NodeSpec = new NodeSpec(node.Lanes.Select(x => x.LaneNode));
            SortedLanes = node.Lanes.OrderBy(x => x.MiddlePosition).ToImmutableList();
            for (int i = 0; i < SortedLanes.Count; i++) SortedLanes[i].Index = i;
            ReferenceFrame = node.PositionProp.Value.CalcReferenceFrame();
            CenterPosition = ReferenceFrame.O + ReferenceFrame.X * NodeSpec.Range.Middle();
        }
    }

    public struct HalfNodeCache {
        public NodeSpec NodeSpec;
        public Transform3 ReferenceFrame;
        public ImmutableArray<HalfLane> SortedLanes;
        public Vector3 CenterPosition;

        internal HalfNodeCache(HalfNode node) {
            var laneList = node.GetLaneList();
            NodeSpec = new NodeSpec(laneList.Select(x => x.LaneNode));
            SortedLanes = laneList.OrderBy(x => x.MiddlePosition).ToImmutableArray();
            ReferenceFrame = node.RoadNode.PositionProp.Value.CalcReferenceFrame();
            if (node.End == NodeEnd.Backward) ReferenceFrame = ReferenceFrame.Around();
            CenterPosition = node.RoadNode.CenterPosition;
        }
    }
}
