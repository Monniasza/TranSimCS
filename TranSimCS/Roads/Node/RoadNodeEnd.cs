using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Property;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Node {
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
            ConnectedSection = new Property<RoadSection?>(null, "connection");
            ConnectedSection.ValueChanged += ConnectedSection_ValueChanged;
        }

        private void ConnectedSection_ValueChanged(object sender, PropertyChangedEventArgs2<RoadSection> e) {
            var oldNode = e.OldValue;
            oldNode?.OnDisconnect(this);
            var newNode = e.NewValue;
            newNode?.OnConnect(this);
        }

        public RoadNodeEnd OppositeEnd => Node.GetEnd(End.Negate());

        //Indexing component for the road node, maintained by the World class
        internal ISet<RoadStrip> connectedSegments = new HashSet<RoadStrip>(); // Connections to other road segments
        public ISet<RoadStrip> ConnectedSegments => new ReadOnlySet<RoadStrip>(connectedSegments); // Expose the connections set

        //Indexing of the road sections
        public readonly Property<RoadSection?> ConnectedSection;

        //Position
        public Property<ObjPos> PositionProp => Node.PositionProp;
        public Vector3 CenterPosition => Node.CenterPosition;
        public Vector3 CenterOffset => Node.CenterOffset;

        public LaneEnd GetLaneEnd(int x) {
            return new LaneEnd(End, Node.Lanes[x]);
        }

        public (float Min, float Max, float LocalLeft, float localRight) Bounds() {
            float min = Node.Lanes[0].LeftPosition;
            float max = Node.Lanes[Node.Lanes.Count-1].RightPosition;
            float lleft = min;
            float lright = max;
            if (End == NodeEnd.Backward) DataUtil.Swap(ref lleft, ref lright);
            return (min, max, lleft, lright);
        }

        //Transform3 _calculatedReferenceFrame;
        public Transform3 CalcReferenceFrame() {
            var frame = PositionProp.Value.CalcReferenceFrame();
            return End == NodeEnd.Backward ? frame.Around() : frame;            
        }

        public void Rotate(int fieldAzimuth, float pitch, float tilt) {
            ((IDraggableObj)Node).Rotate(fieldAzimuth, pitch, tilt);
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

        public MonoGame.Extended.Range<float> Range() {
            var bounds = Bounds();
            return new(bounds.Min, bounds.Max);
        }
    }
}
