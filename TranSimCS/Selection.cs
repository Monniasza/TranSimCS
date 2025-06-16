using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;

namespace TranSimCS {
    public class RoadSelection {
        //In all cases
        public Ray MouseRay; // Ray from the mouse position in the world
        public Vector3 SelectedLanePosition; // Position of the selected lane tag, if any
        public float IntersectionDistance = 0.1f; // Distance to check for intersection with the road segments
        public Object hitObject;

        //Optional selections
        public LaneRange? SelectedLaneTag; // Lane tag that was selected by the mouse ray
        public float SelectedLaneT = 0.5f; // T value for the selected lane tag, if any
        public Bezier3? selectedLaneBezier; // Bezier curve for the selected lane tag
        public SegmentHalf? SelectedRoadHalf; // The road half that the selected lane tag belongs to
        public LaneStrip? SelectedLaneStrip; // The lane strip that the selected lane tag belongs to
        public Lane? SelectedLane;
        public LaneEnd? SelectedLaneEnd;
        public RoadNode? SelectedRoadNode;

        public RoadSelection(LaneStrip laneStrip, float intersectionDistance, Ray mouseRay) {
            hitObject = laneStrip;
            MouseRay = mouseRay;
            IntersectionDistance = intersectionDistance;
            SelectedLanePosition = mouseRay.Position + mouseRay.Direction * intersectionDistance;

            SelectedLaneTag = laneStrip.Tag;
            SelectedLaneStrip = laneStrip; // Store the selected lane strip
            var splines = RoadRenderer.GenerateSplines(SelectedLaneTag.Value);
            Bezier3 averageBezier = (splines.Item1 + splines.Item2) / 2; // Average the two splines
            selectedLaneBezier = averageBezier; // Store the selected lane bezier curve
            SelectedLaneT = Bezier3.FindT(averageBezier, SelectedLanePosition); // Get the T value for the selected lane position
            SelectedRoadHalf =
                SelectedLaneT < InGameMenu.minT ? SegmentHalf.Start : 
                SelectedLaneT > InGameMenu.maxT ? SegmentHalf.End : null; // Determine which half of the road the selected lane tag belongs to
            SelectedLaneEnd =
                SelectedLaneT < InGameMenu.minT ? SelectedLaneStrip?.StartLane :
                SelectedLaneT > InGameMenu.maxT ? SelectedLaneStrip?.EndLane : null;
            SelectedLane = SelectedLaneEnd?.lane;
            SelectedRoadNode = SelectedLane?.RoadNode;
        }

        public RoadSelection(Lane lane, float intersectionDistance, Ray mouseRay) {
            hitObject = lane;
            MouseRay = mouseRay;
            IntersectionDistance = intersectionDistance;
            SelectedLanePosition = mouseRay.Position + mouseRay.Direction * intersectionDistance;

            SelectedLaneTag = null;
            SelectedLaneStrip = null; // Store the selected lane strip
            selectedLaneBezier = null; // Store the selected lane bezier curve
            SelectedLaneT = -1; // Get the T value for the selected lane position
            SelectedRoadHalf = SegmentHalf.Start; // Determine which half of the road the selected lane tag belongs to
            SelectedLane = lane;
            SelectedRoadNode = lane.RoadNode;
        }
    }
}
