using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;

namespace TranSimCS {
    public class RoadSelection {
        public Ray MouseRay; // Ray from the mouse position in the world
        public LaneTag SelectedLaneTag; // Lane tag that was selected by the mouse ray
        public Vector3 SelectedLanePosition; // Position of the selected lane tag, if any
        public float IntersectionDistance = 0.1f; // Distance to check for intersection with the road segments
        public float SelectedLaneT = 0.5f; // T value for the selected lane tag, if any
        public Ray mouseRay; // Ray from the mouse position in the world, used for intersection calculations
        public Bezier3? selectedLaneBezier; // Bezier curve for the selected lane tag
        public SegmentHalf SelectedRoadHalf; // The road half that the selected lane tag belongs to

        public RoadSelection(LaneTag laneTag, float intersectionDistance, Ray mouseRay) {
            MouseRay = mouseRay;
            SelectedLaneTag = laneTag;
            IntersectionDistance = intersectionDistance;
            SelectedLanePosition = mouseRay.Position + mouseRay.Direction * intersectionDistance;
            var splines = RoadRenderer.GenerateSplines(laneTag);
            Bezier3 averageBezier = (splines.Item1 + splines.Item2) / 2; // Average the two splines
            selectedLaneBezier = averageBezier; // Store the selected lane bezier curve
            SelectedLaneT = Bezier3.FindT(averageBezier, SelectedLanePosition); // Get the T value for the selected lane position
            SelectedRoadHalf = SelectedLaneT < 0.5f ? SegmentHalf.Start : SegmentHalf.End; // Determine which half of the road the selected lane tag belongs to
        }
    }
}
