using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class LaneReconcillation {
        public static LaneCreationState? BuildConnections(LaneCreationState startingState, LaneMappings laneMappings, InGameMenu menu) {
            //Get the state
            var destLane = startingState.SnappedLane;
            var world = menu.World;
            if (destLane?.lane == null) {
                //Place the road node 
                //Build the node
                RoadNode roadNode = new RoadNode("", startingState.GeneratedNodePosition);
                var nodeHalf = roadNode.GetHalfNode(startingState.DestinationNodeEnd);
                var newLaneNodes = laneMappings!.EndingLanes;
                var newLanes = new HalfLane[laneMappings.EndingLanes.Length];
                for (int i = 0; i < newLaneNodes.Length; i++) {
                    var laneDef = newLaneNodes[i];
                    var lane = nodeHalf.AddLane(laneDef);
                    newLanes[i] = lane;
                }
                world.Nodes.data.Add(roadNode);

                //Find a new lane to build from
                var passthroughNumber = laneMappings.LaneIndexGoingToSource;
                HalfLane newLaneEnd = newLanes[passthroughNumber].Lane.GetHalfLane(startingState.DestinationNodeEnd);
                Debug.Assert(newLaneEnd != null, "Didn't find a new lane");

                //List previous lanes
                var prevLanes = laneMappings.StartingLanes;

                GenerateLaneConnections(startingState.StartLane.HalfNode, nodeHalf, prevLanes, newLanes, laneMappings.Mappings, menu.configuration.RoadFinish, menu.World);

                return new LaneCreationState(newLaneEnd);
            }

            //There's a connection
            var destinationHalfLane = destLane.Value.ToHalfLane();
            var destinationNode = destinationHalfLane.HalfNode;
            var destIndex = destinationHalfLane.Index;
            var passthroughIndex = laneMappings.LaneIndexGoingToSource;
            /*
             * Dest index       4 3 >2< 1 0   min=-1
             * Source index   0 1 2 >3< 4 5 6 max=5
             * 
             *     >2< 1 0       min=-2
             * 0 1 >2< 3 4 5 6   max=4
             * 
             * >5< 4 3 2 1 0     min=-1
             * >0< 1 2 3 4 5 6   max=5
             * 
             * 4 3 2 1 >0<          min=-4
             *   0 1 2 >3< 4 5 6 7  max=3
             *
             *    >0< min=0
             *    >0< max=0
             *     
             * Goal: (destLength, sourceLength, destIndex, sourceIndex) => (minIndex, maxIndex)
             * Constraints: maxIndex - minIndex = sourceLength - 1
             * 
             * New goal: (destLength, sourceLength, destIndex, sourceIndex) => (minIndex)
             * Unknown formula: a*destLength + b*sourceLength + c*destIndex + d*sourceIndex + e = minIndex
             * 
             * Cases:
             * (5, 7, 2, 3) => -1
             * (3, 7, 2, 2) => -2
             * (6, 7, 5, 0) => -1
             * (5, 8, 0, 3) => -4
             * (1, 1, 0, 0) => 0
             * 
             * Equations:
             * 5a + 7b + 2c + 3d + e = -1
             * 3a + 7b + 2c + 2d + e = -1
             * 6a + 7b + 5c      + e = -1
             * 5a + 8b      + 3d + e = -4
             *  a +  b           + e =  0
             *  
             * Solution: -sourceLength + destIndex + sourceIndex + 1
             */
            var minIndex = destIndex + passthroughIndex - laneMappings.EndingLanes.Length + 1;
            var maxIndex = minIndex + laneMappings.EndingLanes.Length - 1;

            var addLeftLanes = int.Max(-minIndex, 0);
            var addRightLanes = int.Max(0, maxIndex - (destinationNode.LaneCount - 1));

            Debug.WriteLine($"destIndex={destIndex}");
            Debug.WriteLine($"passthroughIndex={passthroughIndex}");
            Debug.WriteLine($"endingLanesLength={laneMappings.EndingLanes.Length}");
            Debug.WriteLine($"laneCount={destinationNode.LaneCount}");
            Debug.WriteLine($"min={minIndex}");
            Debug.WriteLine($"max={maxIndex}");

            //Find how much lanes have to be offset when adding them
            var bounds = destinationNode.OppositeHalf.Bounds;
            if(addLeftLanes > 0) {
                var rightmostLeftLane = laneMappings.EndingLanes[addLeftLanes - 1];
                var leftLaneOffset = bounds.Min - rightmostLeftLane.Bounds.Max;
                for(int i = 0; i < addLeftLanes; i++) {
                    LaneNode ln = laneMappings.EndingLanes[i];
                    destinationNode.OppositeHalf.AddLane(new LaneNode(ln.LaneSpec, ln.CenterPos + leftLaneOffset, ln.ID));
                }
            }
            if (addRightLanes > 0) {
                var leftmostRightLane = laneMappings.EndingLanes[^addRightLanes];
                var rightLaneOffset = bounds.Max - leftmostRightLane.Bounds.Min;
                for (int i = 1; i <= addRightLanes; i++) {
                    LaneNode ln = laneMappings.EndingLanes[^i];
                    destinationNode.OppositeHalf.AddLane(new LaneNode(ln.LaneSpec, ln.CenterPos + rightLaneOffset, ln.ID));
                }
            }

            //Recalculate offsets
            destIndex = destinationHalfLane.Index;
            minIndex = destIndex + passthroughIndex - laneMappings.EndingLanes.Length + 1;
            maxIndex = minIndex + laneMappings.EndingLanes.Length - 1;
            addLeftLanes = int.Max(-minIndex, 0);
            addRightLanes = int.Max(0, maxIndex - (destinationNode.LaneCount - 1));

            Debug.WriteLine($"destIndex={destIndex}");
            Debug.WriteLine($"passthroughIndex={passthroughIndex}");
            Debug.WriteLine($"endingLanesLength={laneMappings.EndingLanes.Length}");
            Debug.WriteLine($"laneCount={destinationNode.LaneCount}");
            Debug.WriteLine($"min={minIndex}");
            Debug.WriteLine($"max={maxIndex}");

            Debug.Assert(addLeftLanes == 0, "Not all required lanes were added");
            Debug.Assert(addRightLanes == 0, "Not all required lanes were added");
            HalfLane[] destLanes = new HalfLane[laneMappings.EndingLanes.Length];
            for (int i = 0; i < laneMappings.EndingLanes.Length; i++) {
                var endingLaneIndex = maxIndex - i;
                destLanes[i] = destinationNode.GetLaneByIndex(endingLaneIndex).OppositeHalf;
            }

            GenerateLaneConnections(startingState.StartLane.HalfNode, destinationNode.OppositeHalf, laneMappings.StartingLanes, destLanes, laneMappings.Mappings, menu.configuration.RoadFinish, menu.World);
            return null;
        }

        public static void GenerateLaneConnections(HalfNode startNode, HalfNode endNode, IList<HalfLane> startLanes, IList<HalfLane> endLanes, IList<LaneMapping> mappings, RoadFinish roadFinish, TSWorld world) {
            //Build the road strip
            RoadStrip road = world.GetOrMakeRoadStrip(startNode.RoadNodeEnd, endNode.OppositeHalf.RoadNodeEnd, roadFinish);
            foreach (var connection in mappings) {
                var startLane = startLanes[connection.StartIndex];
                var endLane = endLanes[connection.EndIndex].OppositeHalf;
                var isBackwards = LaneMappings.IsReverseLaneHeuristic(startLane.Lane);

                //backwards if backwards is clearly preferred or equally preferred but going from the back
                if (isBackwards ^ endNode.End == NodeEnd.Backward) DataUtil.Swap(ref startLane, ref endLane);
                LaneStrip laneStrip = new LaneStrip(startLane.LaneEnd, endLane.LaneEnd);
                var spec = connection.LaneSpec;
                if (isBackwards) spec.Flags = spec.Flags.LongitudinalReverse();
                if (endNode.End == NodeEnd.Backward) spec.Flags = spec.Flags.LongitudinalReverse();
                laneStrip.Spec = spec;
                road.AddLaneStrip(laneStrip);
            }
            world.RoadSegments.data.Add(road);
            
        }
    }
}
