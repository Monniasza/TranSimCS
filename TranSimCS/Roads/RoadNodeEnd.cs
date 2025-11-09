using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Roads {
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
        public Property<RoadSection> ConnectedSection { get; } = new Property<RoadSection>(null, "connection");

        //Position
        public Property<ObjPos> PositionProp => Node.PositionProp;
        public Vector3 CenterPosition => Node.CenterPosition;
        public Vector3 CenterOffset => Node.CenterOffset;

        public LaneEnd GetLaneEnd(int x) {
            return new LaneEnd(End, Node.Lanes[x]);
        }

        public Vector2 Bounds() {
            float lbound = Node.Lanes[0].LeftPosition;
            float rbound = Node.Lanes[Node.Lanes.Count-1].RightPosition;
            if (End == NodeEnd.Backward) (lbound, rbound) = (rbound, lbound);
            return new Vector2(lbound, rbound);
        }

        public Transform3 CalcReferenceFrame() {
            var frame = PositionProp.Value.CalcReferenceFrame();
            return (End == NodeEnd.Backward) ? frame.Around() : frame;
            //it's to be inverted
            
        }

        public void Rotate(int fieldAzimuth, float pitch, float tilt) {
            ((IDraggableObj)Node).Rotate(fieldAzimuth, pitch, tilt);
        }
    }
}
