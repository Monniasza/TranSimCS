using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Roads {
    internal static class Roads {
        /// <summary>
        /// Removes a lane from the specified road node with minimal disruption to road connections.
        /// </summary>
        /// <param name="laneIdx">number of the lane to remove</param>
        /// <param name="resizeWeight">How much for lanes to the left of the removed lane to move right. 0 to only move right lanes to left, 1 to only move left lanes to right</param>
        /// <param name="node">road node to change</param>
        public static void RemoveLane(int laneIdx, RoadNode node, float resizeWeight = 0) {
            if (node.PositionOffsets.Count <= laneIdx || laneIdx < 0) 
                throw new ArgumentOutOfRangeException(nameof(laneIdx), "Lane index is out of range.");
            var world = node.World; // Get the world instance from the node
            var affectedSegments = node.Connections;
            //Split the connections into two lists: those that are before the lane to be removed, those that are after and those that are on the lane to be removed
            var beforeSegs = new List<LaneConnection>();
            var afterSegs = new List<LaneConnection>();
            var onSegs = new List<LaneConnection>();
            foreach (var segment in affectedSegments) {
                if (segment.StartNode == node && segment.EndNode == node) {
                    var segmentSpec = segment.Spec; // Get the segment specification
                    HalfLaneConnectionSpec connSpec;
                    SegmentHalf segmentHalf = SegmentHalf.Start; // Assume the start half is the one we are interested in
                    if (segmentSpec.StartNode == node) {
                        connSpec = segmentSpec.StartHalf; // Get the connection specification for the start node
                        segmentHalf = SegmentHalf.Start; // Start half is the one we are interested in
                    } else if (segmentSpec.EndNode == node) {
                        connSpec = segmentSpec.EndHalf; // Get the connection specification for the end node
                        segmentHalf = SegmentHalf.End; // End half is the one we are interested in
                    } else {
                        node.connections.Remove(segment); // Remove the segment if it does not connect to the node
                        throw new InvalidOperationException("Segment does not connect to the specified node.");
                    }

                    //Categorize the lane based on its index
                    int category = CategorizeLane(laneIdx, connSpec); // Categorize the lane based on its index
                    if (category == 0) beforeSegs.Add(segment); // Lane is before the specified lane
                    else if (category == 1) onSegs.Add(segment); // Lane is on the specified lane
                    else if (category == 2) afterSegs.Add(segment); // Lane is after the specified lane
                }
            }

            float removalWidth = node.PositionOffsets[laneIdx+1] - node.PositionOffsets[laneIdx]; // Get the width of the lane to be removed
            float moveLeft = removalWidth * resizeWeight; // Calculate how much to move left lanes to the right
            float moveRight = removalWidth * (1 - resizeWeight); // Calculate how much to move right lanes to the left

            //Connections that are after the removed lane should be moved left by one spot
            foreach (var segment in afterSegs) {
                var spec = segment.Spec; // Get the segment specification
                var connSpecs = spec.GetHalf(node); // Get the connection specification for the segment half
                var connSpec = connSpecs.Item2; // Get the connection specification for the specified segment half
                var half = connSpecs.Item1;
                // Move the right lanes to the left by one spot
                connSpec.LeftIndex -= 1;
                connSpec.RightIndex -= 1;
                spec[half] = connSpec; // Update the segment specification with the modified connection specification
                segment.Spec = spec; // Update the segment specification
            }

            //Connections that are in the removed lane should only have their higher index lanes moved left by one spot
            foreach (var segment in onSegs) {
                var spec = segment.Spec; // Get the segment specification
                var connSpecs = spec.GetHalf(node); // Get the connection specification for the segment half
                var connSpec = connSpecs.Item2; // Get the connection specification for the specified segment half
                var half = connSpecs.Item1;
                // Move the right lanes to the left by one spot
                if (connSpec.RightIndex > laneIdx) {
                    connSpec.RightIndex -= 1;
                } else {
                    connSpec.LeftIndex -= 1; // If the right index is not greater than the lane index, move the left index
                }
                spec[half] = connSpec; // Update the segment specification with the modified connection specification
                segment.Spec = spec; // Update the segment specification
            }

            //Connections that are before the removed lane should not have their indices changed, but we can still update their connection specifications

            //Finally, we need to remove the lane from the node's position offsets and lane specifications
            node.LaneSpecs.RemoveAt(laneIdx); // Remove the lane specification for the specified lane index
            node.PositionOffsets.RemoveAt(laneIdx + 1); // Remove the position offset for the specified lane index + 1 (because we are removing the lane, we need to remove the next position offset as well)
            var subrangeBefore = node.PositionOffsets.Take(laneIdx + 1).ToList(); // Get the positions before the lane to be removed
            var subrangeAfter = node.PositionOffsets.Skip(laneIdx + 1).ToList(); // Get the positions after the lane to be removed
            // Update the positions of the lanes after the removed lane
            int i;
            for (i = 0; i < subrangeBefore.Count; i++) {
                subrangeBefore[i] += moveRight; // Move the right lanes to the left by the calculated amount
            }
            for(int j = 0; j < subrangeAfter.Count; j++) {
                subrangeBefore[j] -= moveLeft; // Move the left lanes to the right by the calculated amount
            }
            node.PositionOffsets.Clear(); // Clear the lane specifications
            node.PositionOffsets.AddRange(subrangeBefore);
            node.PositionOffsets.AddRange(subrangeAfter); // Add the updated lane specifications back to the node

        }
        // 0 for before, 1 for on lane, 2 for after
        private static int CategorizeLane(int laneIdx, HalfLaneConnectionSpec spec) {
            bool isReversed = spec.IsReversed; // Check if the lane is reversed
            int llimit = spec.LeftIndex; // Get the left lane limit
            int rlimit = spec.RightIndex; // Get the right lane limit
            if (isReversed) {
                // If the lane is reversed, we need to swap the limits
                int temp = llimit;
                llimit = rlimit;
                rlimit = temp;
            }
            if(rlimit < laneIdx) {
                return 0; // Lane is before the specified lane
            } else if (llimit > laneIdx) {
                return 2; // Lane is after the specified lane
            } else {
                return 1; // Lane is on the specified lane
            }
        }
    }

    [Flags]
    public enum VehicleTypes {
        None = 0,
        Car = 1,
        Truck = 2,
        Bus = 4,
        Bicycle = 8,
        Pedestrian = 16,

        // Composite types for convenience  
        Path = Bicycle | Pedestrian,
        MotorVehicles = Car | Truck | Bus,
        Vehicles = MotorVehicles | Bicycle, // All vehicles except parking
        Transport = Vehicles | Pedestrian, // All traffic
        All = -1 // All vehicle and parking types
    }

    [Flags]
    public enum LaneFlags {
        None = 0,
        Forward = 1, // Lane is for forward traffic
        Backward = 2, // Lane is for backward traffic
        LeftTurn = 4, // Lane is for left turns
        RightTurn = 8, // Lane is for right turns
        Straight = 16, // Lane is for straight traffic
        SwitchLeft = 32, // Lane is for switching to the left
        SwitchRight = 64,
        Parking = 128, // Lane is for parking
        Median = 256, // Lane is a median
        Planting = 512, // Lane is for planting
        Platform = 1024, // Lane is a platform (e.g., for buses or trams)
    }
}
