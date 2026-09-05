using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.SilkNet.RoadConstruction;
using TranSimCS.Worlds;

namespace TranSimCS.Mode.RoadConstruction {
    public static class SplitRoad {
        public static void SplitSegment(RoadStrip road, float minT, float maxT) {
            if (float.IsNaN(minT)) throw new ArgumentException("minT must be a valid real number");
            if (float.IsNaN(maxT)) throw new ArgumentException("maxT must be a valid real number");
            if (minT < 0) throw new ArgumentException("minT must be at least 0");
            if (maxT > 1) throw new ArgumentException("maxT must be not more than 1");
            if (minT >= maxT) throw new ArgumentException("minT must be strictly less than maxT");

            if(minT == 0 && maxT == 1) return;

            var world = road.World ?? throw new InvalidOperationException("The road must be a part of a world");
            var spline = road.OrthodistantBasis;

            var newStartNode = road.StartNode;
            var newEndNode = road.EndNode;

            var laneStrips = new LaneStrip[road.Lanes.Count];
            int i = 0;
            foreach(var lane in road.Lanes) {
                laneStrips[i++] = lane;
            }

            //Calculate needed tangent lengths
            var zeroFrame = spline.SampleFull(0, out var zeroVelocity);
            var startFrame = spline.SampleFull(minT, out var startVelocity);
            var endFrame = spline.SampleFull(maxT, out var endVelocity);
            var oneFrame = spline.SampleFull(1, out var oneVelocity);

            var startSpan = minT / 3;
            var midSpan = (maxT - minT) / 3;
            var endSpan = (1 - maxT) / 3;

            //Create new nodes if necessary
            Dictionary<HalfLane, HalfLane> remappings = new();
            if(minT > 0) {
                var newPos = PositionEulerAngles.FromPosTangentLateral(startFrame);
                var oldStartNode = road.StartNode;
                newStartNode = new RoadNode("", newPos).FrontHalf;
                var newSegment = new RoadStrip(oldStartNode, newStartNode.OppositeHalf);
                foreach(var lane in oldStartNode.SortedLanes) {
                    var isOutFromTheLane = LaneMappings.IsReverseLaneHeuristic(lane);
                    var newHalfLane = newStartNode.AddLane(lane.Definition).OppositeHalf;
                    var startLane = lane;
                    var endLane = newHalfLane;
                    if (isOutFromTheLane) DataUtil.Swap(ref startLane, ref endLane);
                    var laneStrip = new LaneStrip(startLane, endLane, lane.LaneSpec);
                    newSegment.AddLaneStrip(laneStrip);
                    remappings[lane] = newHalfLane.OppositeHalf;
                }
                newSegment.SetTangentLengths(zeroVelocity.Length() * startSpan, startVelocity.Length() * startSpan);
                world.RoadSegments.data.Add(newSegment);
            }

            if (maxT < 1) {
                var newPos = PositionEulerAngles.FromPosTangentLateral(endFrame);
                var oldEndNode = road.EndNode;
                newEndNode = new RoadNode("", newPos).RearHalf;
                var newSegment = new RoadStrip(oldEndNode, newEndNode.OppositeHalf);
                foreach (var lane in oldEndNode.SortedLanes) {
                    var isOutFromTheLane = LaneMappings.IsReverseLaneHeuristic(lane);
                    var newHalfLane = newEndNode.AddLane(lane.Definition).OppositeHalf;
                    var startLane = lane;
                    var endLane = newHalfLane;
                    if (isOutFromTheLane) DataUtil.Swap(ref startLane, ref endLane);
                    var laneStrip = new LaneStrip(startLane, endLane, lane.LaneSpec);
                    newSegment.AddLaneStrip(laneStrip);
                    remappings[lane] = newHalfLane.OppositeHalf;
                }
                newSegment.SetTangentLengths(endVelocity.Length() * endSpan, oneVelocity.Length() * endSpan);
                world.RoadSegments.data.Add(newSegment);
            }

            //Rebuild connections
            world.RoadSegments.data.Remove(road);
            var newRoad = new RoadStrip(newStartNode, newEndNode);
            foreach(var strip in laneStrips) {
                var startLane = strip.StartLane;
                var endLane = strip.EndLane;
                var spec = strip.LaneSpec;
                if (remappings.TryGetValue(startLane, out var remappedStart)) startLane = remappedStart;
                if (remappings.TryGetValue(endLane, out var remappedEnd)) endLane = remappedEnd;
                newRoad.AddLaneStrip(new LaneStrip(startLane, endLane, spec));
            }
            newRoad.SetTangentLengths(startVelocity.Length() * midSpan, endVelocity.Length() * midSpan);
            world.RoadSegments.data.Add(newRoad);
        }
    }
}
