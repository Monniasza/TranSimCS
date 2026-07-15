using System;
using System.Diagnostics;
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
        public readonly HalfLane StartLane;
        public Range<float> StartRange;
        public Range<float> EndRange;

        //EXTERNAL SOURCE STATE
        public Vector3 TargetPosition;
        public RoadMode SplineMode;
        public Alignment Alignment;

        //GENERATED STATE
        public Strip GeneratedSplines;
        public PositionEulerAngles GeneratedNodePosition;
        public float DeltaOffset;
        public LaneEnd? SnappedLane;
        public NodeEnd DestinationNodeEnd;

        //DERIVED STATE
        public Bezier3 CenterLine => GeneratedSplines.Middle;

        //CONSTRUCTOR
        public LaneCreationState(HalfLane laneEnd) {
            ArgumentNullException.ThrowIfNull(laneEnd, nameof(laneEnd));
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

            DeltaOffset = 0;
            SnappedLane = null;
            DestinationNodeEnd = StartLane.End;

            //Shared variables
            var laneRange = StartLane.Bounds;
            var (alignmentMulLeft, alignmentMulRight) = Alignment.GetAlignments();
            float referenceIndex = alignmentMulLeft * laneRange.Min + alignmentMulRight * laneRange.Max;
            var startingPositionRef = LineEnd.calcLineEnd(StartLane.HalfNode, referenceIndex);
            var startTangent = startingPositionRef.Tangential;
            var startLateral = startingPositionRef.Lateral;
            var startLanePos = startingPositionRef.Position;
            var startWidth = StartLane.Width;

            var selectedRoadLane = menu.MouseOver?.As<LaneEnd>() ?? default;
            if (selectedRoadLane.lane != null && selectedRoadLane.ToHalfLane() != StartLane) {
                //Picked a lane. Match it to the target road node
                var targetCenterPos = -selectedRoadLane.ToHalfLane().MiddlePosition;
                var sourceCenterPos = StartLane.MiddlePosition;
                DeltaOffset = targetCenterPos - sourceCenterPos;
                GeneratedNodePosition = selectedRoadLane.GetRoadNode().InversePositionProp.Value;
                SnappedLane = selectedRoadLane;
                DestinationNodeEnd = selectedRoadLane.end;
            } else {
                //Create a synthetic end
                Plane selectionPlane = menu.ReferencePlane;
                TargetPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, selectionPlane);
                if (menu.CheckSnap.Checked)
                    //Snap the position
                    TargetPosition = menu.configuration.SnapGrid.Snap(TargetPosition);
                RoadPlan plan = new RoadPlan {
                    startLateral = startLateral,
                    endLateral = startLateral,
                    startPos = startLanePos,
                    endPos = TargetPosition,
                    startTangent = startTangent,
                    endTangent = startTangent,
                    menu = menu
                };
                plan.Align(Alignment, startWidth);
                SplineMode.CreateValues(plan);
                plan.Align(Alignment.Inverse(), startWidth);

                Debug.Assert(plan.endPos.IsFinite(), "Invalid end position");
                Debug.Assert(plan.endLateral.IsFinite(), "Invalid end lateral");
                Debug.Assert(plan.endTangent.IsFinite(), "Invalid end tangent");

                //Flatten tilt or inclination
                if (stripTools.flattenTilt.Checked) plan.endLateral = plan.endLateral.ToX0Z().Normalized();
                if (stripTools.flattenIncline.Checked) plan.endTangent = plan.endTangent.ToX0Z().Normalized();

                if (!(plan.endTangent.IsFinite() && plan.endLateral.IsFinite())) {
                    plan.endTangent = plan.startTangent;
                    plan.endLateral = plan.startLateral;
                }

                var correctedPosition = plan.endPos - plan.endLateral * referenceIndex;

                //Calculate the NodePosition
                var newNodePosition = PositionEulerAngles.FromPosTangentLateral(correctedPosition, plan.endTangent, plan.endLateral);
                if (StartLane.End == NodeEnd.Backward) newNodePosition.Azimuth += int.MinValue;
                if (stripTools.flattenTilt.Checked) newNodePosition.Tilt = 0;
                if (stripTools.flattenIncline.Checked) newNodePosition.Inclination = 0;
                GeneratedNodePosition = newNodePosition;
            }

            var refframe = GeneratedNodePosition.CalcReferenceFrame();
            var endPosition = refframe.O;
            var endTangent = refframe.Z;
            var endLateral = refframe.X;
            if (DestinationNodeEnd == NodeEnd.Backward) {
                endLateral *= -1;
                endTangent *= -1;
            }
            endPosition += DeltaOffset * endLateral;
            GeneratedNodePosition.Position = endPosition;
            Bezier3 lbound = GeometryUtils.GenerateJoinSpline(startLanePos + startLateral * (StartRange.Min - referenceIndex), endPosition + endLateral * EndRange.Min, startTangent, -endTangent);
            Bezier3 rbound = GeometryUtils.GenerateJoinSpline(startLanePos + startLateral * (StartRange.Max - referenceIndex), endPosition + endLateral * EndRange.Max, startTangent, -endTangent);
            //Assert spline validity
            VectorMethods.CheckSpline(lbound, "left");
            VectorMethods.CheckSpline(rbound, "right");
            GeneratedSplines = new(lbound, rbound);
        }
    
    }
}
