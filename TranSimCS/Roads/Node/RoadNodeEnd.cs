using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Property;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
    /**
     * TODO Implement HalfNode
     */
    public class RoadNodeEnd: IPosition, IRoadElement {
        //ROAD ELEMENT
        public Guid Guid => Node.Guid;
        public Lane? GetLane() => null;
        public LaneStrip? GetLaneStrip() => null;
        public RoadNode? GetRoadNode() => Node;
        public RoadStrip? GetRoadStrip() => null;
        public int XDiscriminant() => 0;
        public int ZDiscriminant() => End.Discriminant();
        public LaneEnd? GetLaneEnd() => null;
        public RoadNodeEnd? GetNodeEnd() => this;

        //Constructor
        public readonly NodeEnd End;
        public readonly RoadNode Node;
        internal RoadNodeEnd(NodeEnd end, RoadNode node) {
            End = end;
            Node = node;
        }

        public RoadNodeEnd OppositeEnd => Node.GetEnd(End.Negate());

        //Indexing component for the road node, maintained by the World class
        internal ISet<RoadStrip> connectedSegments = new HashSet<RoadStrip>(); // Connections to other road segments
        public ISet<RoadStrip> ConnectedSegments => new ReadOnlySet<RoadStrip>(connectedSegments); // Expose the connections set

        //Indexing of the road sections
        public Property<RoadSection?> ConnectedSection => HalfNode.ConnectedSection;

        //Position
        public Property<PositionEulerAngles> PositionProp => Node.PositionProp;
        public Vector3 CenterPosition => Node.CenterPosition;

        public LaneEnd GetLaneEnd(int x) {
            return new LaneEnd(End, Node.SortedLanes[x]);
        }

        public (float Min, float Max, float LocalLeft, float LocalRight) Bounds() {
            var minMax = Range();
            float min = minMax.Min;
            float max = minMax.Max;
            float lleft = min;
            float lright = max;
            if (End == NodeEnd.Backward) DataUtil.Swap(ref lleft, ref lright);
            return (min, max, lleft, lright);
        }

        //Transform3 _calculatedReferenceFrame;
        public Transform3 CalcReferenceFrame() { //TODO this needs to be cached
            var frame = Node.ReferenceFrame;
            return End == NodeEnd.Backward ? frame.Around() : frame;            
        }

        public RoadSection GetOrCreateSection() {
            var section = ConnectedSection.Value;
            if (section == null) {
                section = new RoadSection();
                ConnectedSection.Value = section;
                Node.World.RoadSections.data.Add(section);
            }
            return section;
        }

        public MonoGame.Extended.Range<float> Range() => Node.Bounds;
        public HalfNode HalfNode => Node.GetHalfNode(End);
    }
}
