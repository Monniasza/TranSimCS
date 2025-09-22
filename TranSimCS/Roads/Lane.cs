using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {

    public class Lane: IDraggableObj {
        /// <summary>
        /// The parent <see cref="RoadNode"/>
        /// </summary>
        public RoadNode RoadNode { get; internal set; }
        private LaneSpec _spec;
        /// <summary>
        /// Specification of the lane, including properties like color, type, etc.
        /// The width here is ignored when set, but it's returned with the proper value when get.
        /// </summary>
        public LaneSpec Spec { get {
            _spec.Width = Width;
            return _spec;
        } set {
            if (value == _spec) return;
            _spec = value;
            RoadNode?.InvalidateMesh();
            foreach(var connection in Connections) 
                connection.InvalidateMesh();
        }} 
        public float LeftPosition { get; set; } // Left position of the lane relative to the road node
        public float RightPosition { get; set; } // Right position of the lane relative to the road node
        public int Index { get; internal set; } // Index of the lane in the road node's lane list
        public float MiddlePosition => (LeftPosition + RightPosition) / 2; // Middle position of the lane, calculated as the average of left and right positions
        public float Width => RightPosition - LeftPosition;
        public LaneEnd Rear => new LaneEnd(NodeEnd.Backward, this);
        public LaneEnd Front => new LaneEnd(NodeEnd.Forward, this);

        //Positioning utilities
        public void Align(float t, float pos, float width = -1) {
            if (width < 0) width = Width;
            LeftPosition = pos - t * width;
            RightPosition = LeftPosition + width;
        }

        public LaneEnd GetEnd(NodeEnd end) {
            return end.GetConditional(Rear, Front);
        }

        //Indexing
        internal ISet<LaneStrip> connections = new HashSet<LaneStrip>(); // Set of lane strips that this lane is connected to
        public ISet<LaneStrip> Connections => new ReadOnlySet<LaneStrip>(connections); // Read-only set of lane strips that this lane is connected to

        //Dragging
        public void Drag(Vector3 vector, Vector3 dragFrom) => ((IDraggableObj)RoadNode).Drag(vector, dragFrom);
    }
}
