using System;
using System.Diagnostics;
using System.Numerics;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.SilkNet;
using TranSimCS.Spline;
using TranSimCS.Tools.RoadConstruction;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class LaneCreationState {
        //SOURCE STATE
        public readonly HalfLane StartLane;
        public Interval<float> StartRange;
        public Interval<float> EndRange;

        //EXTERNAL SOURCE STATE
        public Vector3 TargetPosition;
        public RoadMode SplineMode;
        public Alignment Alignment;

        //GENERATED STATE
        public SplineStrip GeneratedSplines;
        public PositionEulerAngles GeneratedNodePosition;
        public float DeltaOffset;
        public IRoadElement? SnappedLane;
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
        public void Generate(SilkNetTest menu) {
            SplineMode = menu.SegmentPresets.RoadMode;
            Alignment = menu.SegmentPresets.Alignment;

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
            var selectedRoadLane = menu.MouseOver?.As<IRoadElement>();
            if(selectedRoadLane is AddLaneSelection als) {
                //Picked an Add Lane Selection
                var targetCenterPos = -(als.CalculateOffset(StartLane.Width/2)) * als.ZDiscriminant();
                var sourceCenterPos = StartLane.MiddlePosition;
                DeltaOffset = targetCenterPos - sourceCenterPos;
                GeneratedNodePosition = als.GetRoadNode().InversePositionProp.Value;
                SnappedLane = selectedRoadLane;
                DestinationNodeEnd = als.nodeEnd.End;
            }else if (selectedRoadLane is HalfLane laneEnd && laneEnd != StartLane) {
                //Picked a lane or an Add Lane Selection. Match it to the target road node
                var targetCenterPos = -laneEnd.MiddlePosition;
                var sourceCenterPos = StartLane.MiddlePosition;
                DeltaOffset = targetCenterPos - sourceCenterPos;
                GeneratedNodePosition = laneEnd.GetRoadNode().InversePositionProp.Value;
                SnappedLane = selectedRoadLane;
                DestinationNodeEnd = laneEnd.End;
            } else {
                //Create a synthetic end
                Plane selectionPlane = menu.snappingGrid.CreateSnappingPlane();
                TargetPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, selectionPlane);
                if (menu.SnappingEnabled)
                    //Snap the position
                    TargetPosition = menu.snappingGrid.Snap(TargetPosition);
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
                if (menu.SegmentPresets.IsTiltFlat) plan.endLateral = plan.endLateral.ToX0Z().Normalized();
                if (menu.SegmentPresets.IsInclineFlat) plan.endTangent = plan.endTangent.ToX0Z().Normalized();

                if (!(plan.endTangent.IsFinite() && plan.endLateral.IsFinite())) {
                    plan.endTangent = plan.startTangent;
                    plan.endLateral = plan.startLateral;
                }

                var correctedPosition = plan.endPos - plan.endLateral * referenceIndex;

                //Calculate the NodePosition
                var newNodePosition = PositionEulerAngles.FromPosTangentLateral(correctedPosition, plan.endTangent, plan.endLateral);
                if (StartLane.End == NodeEnd.Backward) newNodePosition.Azimuth += int.MinValue;
                if (menu.SegmentPresets.IsTiltFlat) newNodePosition.Tilt = 0;
                if (menu.SegmentPresets.IsInclineFlat) newNodePosition.Inclination = 0;
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
