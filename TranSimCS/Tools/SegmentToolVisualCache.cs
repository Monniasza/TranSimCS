using System;
using System.Collections.Generic;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class SegmentToolVisualCache {
        public Inputs InputData { get; private set; }
        public RoadStripData GeneratedData { get; private set; }

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

            //Construct
            RoadDataBuilder rdb = new();

            rdb.StartPos = inputs.StartPosition;
            rdb.EndPos = inputs.EndPosition;
            rdb.StartLanes = inputs.LaneMappings.StartingLanes.ToNodeSpec();
            rdb.EndLanes = inputs.LaneMappings.EndingLanes.ToNodeSpec();
            var dualRange = new DualRange(rdb.StartLanes.Range, rdb.EndLanes.Range);
            var indexSpline = SplineAlgorithms.GenerateSegmentSplinedUsingAlg(rdb.StartPos.CalcReferenceFrame(), rdb.EndPos.CalcReferenceFrame(), dualRange, SplineAlgorithms.AnisotropicSpline);
            rdb.IndexSpline = indexSpline;
            rdb.RoadFinish = inputs.RoadFinish;
            foreach(var connection in inputs.LaneMappings.Mappings) {
                var startLane = inputs.LaneMappings.StartingLanes[connection.StartIndex];
                var endLane = inputs.LaneMappings.EndingLanes[connection.EndIndex];
                var spec = connection.LaneSpec;
                rdb.AddConnection(startLane.LaneNode, endLane, spec);
            }
            GeneratedData = rdb.Create();
        }
    }
}