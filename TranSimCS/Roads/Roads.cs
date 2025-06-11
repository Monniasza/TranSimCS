using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var affectedSegments = node.Connections.ToArray(); //defesive copy of the connections to avoid modifying the collection while iterating

            //Split the connections into two lists: those that are before the lane to be removed, those that are after and those that are on the lane to be removed
            foreach (var segment in affectedSegments) {
                var spec = segment.Spec; // Get the segment specification
                var halfSpecs = spec.GetHalf(node); // Get the connection specification for the segment half
                var halfSpec = halfSpecs.Item2; // Get the connection specification for the specified segment half
                var half = halfSpecs.Item1;

                //Categorize the lane based on its index
                int category = CategorizeLane(laneIdx, halfSpec); // Categorize the lane based on its index
                Debug.Print($"Categorized lane {laneIdx} as {category} for segment {segment.StartNode.Id} to {segment.EndNode.Id} on node {node.Id}");

                // Move the right lanes to the left by one spot
                if (halfSpec.LeftIndex > laneIdx) halfSpec.LeftIndex -= 1;
                if (halfSpec.RightIndex > laneIdx) halfSpec.RightIndex -= 1;

                //If the connection is to be narrowed to 0 lanes, we need to remove it
                if (halfSpec.LeftIndex == halfSpec.RightIndex) {
                    world.RoadSegments.Remove(segment); // Remove the segment from the world
                    continue; // Skip further processing for this segment
                }

                spec = spec.SetHalf(half, halfSpec); // Update the segment specification with the modified connection specification
                segment.Spec = spec; // Update the segment specification
                segment.InvalidateMesh(); // Invalidate the mesh of the segment to force a redraw
            }

            float removalWidth = node.PositionOffsets[laneIdx+1] - node.PositionOffsets[laneIdx]; // Get the width of the lane to be removed
            float moveLeft = removalWidth * resizeWeight; // Calculate how much to move left lanes to the right
            float moveRight = removalWidth * (1 - resizeWeight); // Calculate how much to move right lanes to the left

            //Connections that are before the removed lane should not have their indices changed, but we can still update their connection specifications

            //Finally, we need to remove the lane from the node's position offsets and lane specifications
            node.LaneSpecs.RemoveAt(laneIdx); // Remove the lane specification for the specified lane index
            node.PositionOffsets.RemoveAt(laneIdx + 1); // Remove the position offset for the specified lane index + 1 (because we are removing the lane, we need to remove the next position offset as well)
            var subrangeBefore = node.PositionOffsets.Take(laneIdx + 1).ToList(); // Get the positions before the lane to be removed
            var subrangeAfter = node.PositionOffsets.Skip(laneIdx + 1).ToList(); // Get the positions after the lane to be removed
            
            // Update the positions of the lanes after the removed lane
            for(int i = 0; i < subrangeBefore.Count; i++) {
                node.PositionOffsets[i] += moveRight; // Move the left lanes to the right by the calculated amount
            }
            for(int i = laneIdx + 1; i < node.PositionOffsets.Count; i++) {
                node.PositionOffsets[i] -= moveLeft; // Move the right lanes to the left by the calculated amount
            }

        }
        // 0 for before, 1 for on lane, 2 for after
        private static int CategorizeLane(int laneIdx, HalfLaneConnectionSpec spec) {
            int llimit = spec.LeftIndex; // Get the left lane limit
            int rlimit = spec.RightIndex; // Get the right lane limit

            if(rlimit < laneIdx && rlimit < laneIdx) {
                return 0; // Lane is before the specified lane
            } else if (llimit > laneIdx && rlimit > laneIdx) {
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
