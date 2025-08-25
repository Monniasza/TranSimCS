using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;
using TranSimCS.Spline;

namespace TranSimCS.Menus.InGame {
    public struct AddLaneSelection {
        public sbyte side; //-1 for left, 1 for right
        public float position;
        public RoadNodeEnd nodeEnd;

        public AddLaneSelection(sbyte side, float position, RoadNodeEnd nodeEnd) {
            this.side = side;
            this.position = position;
            this.nodeEnd = nodeEnd;
        }

        public Vector2 CalculateOffsets(float width) {
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
            Lane newLane = new Lane(nodeEnd.Node);
            var positions = CalculateOffsets(spec.Width);
            newLane.LeftPosition = positions.X;
            newLane.RightPosition = positions.Y;
            newLane.Spec = spec;
            nodeEnd.Node.AddLane(newLane);
            return newLane.GetEnd(nodeEnd.End);
        }
    }
    public class RoadSelection {
        //In all cases
        public Ray MouseRay; // Ray from the mouse position in the world
        public Vector3 SelectedLanePosition; // Position of the selected lane tag, if any
        public float IntersectionDistance = 0.1f; // Distance to check for intersection with the road segments
        public object hitObject;

        //Optional selections
        public LaneRange? SelectedLaneTag; // Lane tag that was selected by the mouse ray
        public float SelectedLaneT = 0.5f; // T value for the selected lane tag, if any
        public Bezier3? selectedLaneBezier; // Bezier curve for the selected lane tag
        public SegmentHalf? SelectedRoadHalf; // The road half that the selected lane tag belongs to
        public LaneStrip SelectedLaneStrip; // The lane strip that the selected lane tag belongs to
        public NodeEnd? SelectedNodeSide;
        public LaneEnd? SelectedLaneEnd;
        public Lane SelectedLane;
        public RoadNode SelectedRoadNode;

        public LaneStripEnd? LaneStripEnd => (SelectedRoadHalf == null || SelectedLaneStrip == null) ? null : new LaneStripEnd(SelectedLaneStrip, SelectedRoadHalf.Value);

        public RoadSelection(LaneStrip laneStrip, float intersectionDistance, Ray mouseRay) {
            hitObject = laneStrip;
            MouseRay = mouseRay;
            IntersectionDistance = intersectionDistance;
            SelectedLanePosition = mouseRay.Position + mouseRay.Direction * intersectionDistance;

            SelectedLaneTag = laneStrip.Tag;
            SelectedLaneStrip = laneStrip; // Store the selected lane strip
            var splines = RoadRenderer.GenerateSplines(SelectedLaneTag.Value);
            Bezier3 averageBezier = (splines.Item1 + splines.Item2) / 2; // Average the two splines
            SelectedLaneT = Bezier3.FindT(averageBezier, SelectedLanePosition); // Get the T value for the selected lane position
            selectedLaneBezier = averageBezier; // Store the selected lane bezier curve
            SelectedLaneEnd =
                SelectedLaneT < InGameMenu.minT ? laneStrip?.StartLane :
                SelectedLaneT > InGameMenu.maxT ? laneStrip?.EndLane.OppositeEnd : null;
            SelectedLane = SelectedLaneEnd?.lane; //somewhat this is null
             SelectedRoadHalf =
                SelectedLaneT < InGameMenu.minT ? SegmentHalf.Start : 
                SelectedLaneT > InGameMenu.maxT ? SegmentHalf.End : null; // Determine which half of the road the selected lane tag belongs to
            SelectedRoadNode = SelectedLane?.RoadNode;
            SelectedNodeSide = SelectedLaneEnd?.end;

            hitObject = LaneStripEnd ?? hitObject;
        }

        public RoadSelection(LaneEnd lane, float intersectionDistance, Ray mouseRay) {
            hitObject = lane;
            MouseRay = mouseRay;
            IntersectionDistance = intersectionDistance;
            SelectedLanePosition = mouseRay.Position + mouseRay.Direction * intersectionDistance;

            SelectedLaneTag = null;
            SelectedLaneStrip = null; // Store the selected lane strip
            selectedLaneBezier = null; // Store the selected lane bezier curve
            SelectedLaneT = -1; // Get the T value for the selected lane position
            SelectedRoadHalf = null; // Determine which half of the road the selected lane tag belongs to
            SelectedLane = lane.lane;
            SelectedRoadNode = lane.lane.RoadNode;
            SelectedLaneEnd = lane;
            SelectedNodeSide = lane.end;
        }
    }
}
