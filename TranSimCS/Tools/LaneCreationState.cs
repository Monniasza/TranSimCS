using System;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class LaneCreationState {
        //SOURCE STATE
        public readonly LaneEnd StartLane;
        public Range<float> StartRange;
        public Range<float> EndRange;

        //EXTERNAL SOURCE STATE
        public Vector3 TargetPosition;
        public RoadMode SplineMode;
        public Alignment Alignment;

        //GENERATED STATE
        public Strip GeneratedSplines;
        public PositionEulerAngles GeneratedNodePosition;
        public RoadPlan RoadPlan;

        //DERIVED STATE
        public Bezier3 CenterLine => GeneratedSplines.Middle;

        //CONSTRUCTOR
        public LaneCreationState(LaneEnd laneEnd) {
            ArgumentNullException.ThrowIfNull(laneEnd.lane, nameof(laneEnd.lane));
            StartLane = laneEnd;
        }

        //REPRESENTATION
        public String GenerateDescription() =>
            $"Creating a segment. Chord-length: {CenterLine.ChordLength()}, arc-length: {CenterLine.ArcLength()}";
        
        //GENERATION
        public void Generate(InGameMenu menu) {
            var stripTools = menu.ToolsPanel.GetPanel<StripTools>(ToolAttribs.showRoadTools);
            SplineMode = menu.RoadCreationTool.Mode;
            Alignment = stripTools.AlignmentProp.Value;

            var (alignmentMulLeft, alignmentMulRight) = Alignment.GetAlignments();
            var laneRange = StartLane.lane.Bounds;
            var referenceIndex = alignmentMulLeft * laneRange.Min + alignmentMulRight * laneRange.Max;
            var startingPositionRef = LineEnd.calcLineEnd(StartLane.RoadNodeEnd, referenceIndex);
            var startTangent = startingPositionRef.Tangential;
            var startLateral = startingPositionRef.Lateral;
            startLateral *= StartLane.end.Discriminant();
            var startPos = startingPositionRef.Position;
            var startWidth = StartLane.lane.Width;
            Plane selectionPlane = menu.ReferencePlane;
            TargetPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, selectionPlane);
            if (menu.CheckSnap.Checked) 
                //Snap the position
                TargetPosition = menu.configuration.SnapGrid.Snap(TargetPosition);
            RoadPlan plan = new RoadPlan {
                startLateral = startLateral,
                endLateral = startLateral,
                startPos = startPos,
                endPos = TargetPosition,
                startTangent = startTangent,
                endTangent = startTangent,
                menu = menu
            };
            plan.Align(Alignment, startWidth);
            //Apply the road mode
            SplineMode.CreateValues(plan);
            plan.Align(Alignment.Inverse(), startWidth);
            RoadPlan = plan;

            //Flatten tilt or inclination
            if (stripTools.flattenTilt.Checked) plan.endLateral = plan.endLateral.ToX0Z().Normalized();
            if (stripTools.flattenIncline.Checked) plan.endTangent = plan.endTangent.ToX0Z().Normalized();

            var correctedLateral = plan.endLateral * StartLane.end.Discriminant();
            var correctedPosition = plan.endPos - correctedLateral * StartLane.lane.LeftPosition;

            //Calculate the NodePosition
            var newNodePosition = PositionEulerAngles.FromPosTangentLateral(correctedPosition, plan.endTangent, correctedLateral);
            if (StartLane.end == NodeEnd.Backward) newNodePosition.Azimuth += int.MinValue;
                GeneratedNodePosition = newNodePosition;
            if (stripTools.flattenTilt.Checked) newNodePosition.Tilt = 0;
            if (stripTools.flattenIncline.Checked) newNodePosition.Inclination = 0;

            Bezier3 lbound = GeometryUtils.GenerateJoinSpline(startPos + plan.startLateral * (StartRange.Min - referenceIndex), plan.endPos + plan.endLateral * (EndRange.Min - referenceIndex), startTangent, -plan.endTangent);
            Bezier3 rbound = GeometryUtils.GenerateJoinSpline(startPos + plan.startLateral * (StartRange.Max - referenceIndex), plan.endPos + plan.endLateral * (EndRange.Max - referenceIndex), startTangent, -plan.endTangent);
            if (StartLane.end == NodeEnd.Backward) DataUtil.Swap(ref lbound, ref rbound);
            GeneratedSplines = new(lbound, rbound);
        }
    
    }
}
