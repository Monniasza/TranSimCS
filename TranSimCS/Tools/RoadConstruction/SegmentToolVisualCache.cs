using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.StripData;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Tools.RoadConstruction {
    public class SegmentToolVisualCache {
        public Inputs InputData { get; private set; }
        public RoadStripData? GeneratedData { get; private set; }

        public struct Inputs : IEquatable<Inputs> {
            public PositionEulerAngles StartPosition;
            public PositionEulerAngles EndPosition;
            public LaneMappings LaneMappings;
            public RoadFinish RoadFinish;

            public Inputs(PositionEulerAngles startPosition, PositionEulerAngles endPosition, LaneMappings laneMappings, RoadFinish roadFinish) {
                StartPosition = startPosition;
                EndPosition = endPosition;
                LaneMappings = laneMappings;
                RoadFinish = roadFinish;
            }

            public override bool Equals(object? obj) {
                return obj is Inputs inputs && Equals(inputs);
            }

            public bool Equals(Inputs other) {
                return StartPosition.Equals(other.StartPosition) &&
                       EndPosition.Equals(other.EndPosition) &&
                       EqualityComparer<LaneMappings>.Default.Equals(LaneMappings, other.LaneMappings) &&
                       EqualityComparer<RoadFinish>.Default.Equals(RoadFinish, other.RoadFinish);
            }

            public override int GetHashCode() {
                return HashCode.Combine(StartPosition, EndPosition, LaneMappings, RoadFinish);
            }

            public static bool operator ==(Inputs left, Inputs right) {
                return left.Equals(right);
            }

            public static bool operator !=(Inputs left, Inputs right) {
                return !(left == right);
            }
        }

        public SegmentToolVisualCache(Inputs inputs) {
            //Validate
            ArgumentNullException.ThrowIfNull(inputs.LaneMappings, nameof(inputs.LaneMappings));

            InputData = inputs;

            //Reject if the input position is invalid or the segment is too short
            if(!inputs.EndPosition.IsFinite()) return;
            var startFrame = inputs.StartPosition.CalcReferenceFrame();
            var endFrame = inputs.EndPosition.CalcReferenceFrame();
            foreach (var laneMapping in inputs.LaneMappings.Mappings) {
                var startLane = inputs.LaneMappings.StartingLanes[laneMapping.StartIndex];
                var endLane = inputs.LaneMappings.EndingLanes[laneMapping.EndIndex];
                var startPos = startFrame.O + startFrame.X * startLane.MiddlePosition;
                var endPos = endFrame.O + endFrame.X * endLane.CenterPos;
                var dist = Vector3.Distance(startPos, endPos);
                if (dist < 0.1f) return; //Lanes too close
            }

            //Construct
            RoadDataBuilder rdb = new();

            rdb.StartPos = inputs.StartPosition;
            rdb.EndPos = inputs.EndPosition;
            rdb.StartLanes = inputs.LaneMappings.StartingLanes.ToNodeSpec();
            rdb.EndLanes = inputs.LaneMappings.EndingLanes.ToNodeSpec();
            var dualRange = new DualRange(rdb.StartLanes.Range, rdb.EndLanes.Range);
            var indexSpline = SplineAlgorithms.GenerateSegmentSplinedUsingAlg(startFrame, endFrame, dualRange, SplineAlgorithms.AnisotropicSpline);
            rdb.IndexSpline = indexSpline;
            rdb.RoadFinish = inputs.RoadFinish;
            foreach (var connection in inputs.LaneMappings.Mappings) {
                var startLane = inputs.LaneMappings.StartingLanes[connection.StartIndex];
                var endLane = inputs.LaneMappings.EndingLanes[connection.EndIndex];
                var spec = connection.LaneSpec;
                rdb.AddConnection(startLane.LaneNode, endLane, spec);
            }
            GeneratedData = rdb.Create();
        }
    }
}