using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;

namespace TranSimCS
{
    internal static class Generator {
        public static void GenerateLanes(int count, RoadNode node, LaneSpec spec, float offset = 0) {
            if (count < 1) throw new ArgumentException("Count must be at least 1.", nameof(count));
            // Clear existing position offsets
            node.ClearLanes();
            //Generate lane specifications for each lane
            for (int i = 0; i < count; i++) {
                var lposition = offset + i * (float)spec.Width; // Calculate the left position for the lane
                var rposition = lposition + (float)spec.Width; // Calculate the right position for the lane
                Lane lane = new Lane(node) {
                    Spec = spec, // Set the lane specification
                    LeftPosition = lposition, // Set the left position
                    RightPosition = rposition, // Set the right position
                    Index = i // Set the index of the lane
                };
                node.AddLane(lane); // Add the lane to the road node
            }
        }

        public static void GenerateLanes(int count, RoadNode node, float laneWidth = 3.5f, float offset = 0){
            var laneSpec = LaneSpec.Default;
            laneSpec.Width = laneWidth;
            GenerateLanes(count, node, laneSpec, offset);
        }

        /// <summary>
        /// Generates a road strip connecting two road nodes with specified lane indices and shifts.
        /// </summary>
        /// <param name="start">starting road node</param>
        /// <param name="lstartIdx">left start index, inclusive</param>
        /// <param name="rstartIdx">right start index, exclusive</param>
        /// <param name="end">ending road node</param>
        /// <param name="lendIdx">left end index, inclusive</param>
        /// <param name="rendIdx">right end index, exclusive</param>
        /// <param name="shls">how many lanes close from the left on the beginning</param>
        /// <param name="shle">how many lanes open from the left to the end</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static RoadStrip GenerateLaneConnections(RoadNodeEnd start, int lstartIdx, int rstartIdx, RoadNodeEnd end, int lendIdx, int rendIdx, int shls = 0, int shle = 0){
            //Calculate lane balances
            int startingLanes = rstartIdx - lstartIdx; // How many lanes are open at the start node
            int endingLanes = rendIdx - lendIdx; // How many lanes are open at the start node
            int leftShift = shle - shls; // How many lanes open to the left (negative means that the left lanes close)
            int rightShift = endingLanes - startingLanes - leftShift; // How many lanes open to the right (negative means that the right lanes close)
            int closingLeftLanes = Math.Max(0, -leftShift); // How many lanes close to the left
            int openingLeftLanes = Math.Max(0, leftShift); // How many lanes open to the left
            int closingRightLanes = Math.Max(0, -rightShift); // How many lanes close to the right
            int openingRightLanes = Math.Max(0, rightShift); // How many lanes open to the right

            //Calculate unchanging lanes
            int unchangingLanesStartLeft = lstartIdx + closingLeftLanes;
            int unchangingLanesStartRight = rstartIdx - closingRightLanes;
            int unchangingLanesEndLeft = lendIdx + openingLeftLanes;
            int unchangingLanesEndRight = rendIdx - openingRightLanes;
            int unchangingLanesCount = unchangingLanesStartRight - unchangingLanesStartLeft; // How many lanes remain unchanged

            int minCount = Math.Min(startingLanes, endingLanes); // Determine the minimum number of lanes between left and right

            if (startingLanes < 1) throw new ArgumentException("Lane count must be at least 1.", nameof(startingLanes));
            if (endingLanes < 1) throw new ArgumentException("Lane count must be at least 1.", nameof(endingLanes));
            if (Math.Abs(leftShift) >= minCount) throw new ArgumentException("Shift left absolute must be less than the smaller number of lanes", nameof(leftShift));
            if (Math.Abs(rightShift) >= minCount) throw new ArgumentException("Shift right absolute must be less than the smaller number of lanes", nameof(rightShift));

            RoadStrip strip = new RoadStrip(start, end); // Create a new road strip to hold the connections

            //Generate the left changing section of the road
            for(int i = 0; i < closingLeftLanes; i++) 
                JoinLanesByIndices(strip, lstartIdx+i, lendIdx);
            for(int i = 0; i < openingLeftLanes; i++)
                JoinLanesByIndices(strip, lstartIdx, lendIdx+i);

            // Generate lane connections based on the calculated indices and shifts
            for(int i = 0; i < unchangingLanesCount; i++) {
                int startIdx = unchangingLanesStartLeft + i; // Calculate the starting index for the lane
                int endIdx = unchangingLanesEndLeft + i; // Calculate the ending index for the lane
                JoinLanesByIndices(strip, startIdx, endIdx);
            }

            //Generate the right changing section of the road
            for(int i = 0; i < closingRightLanes; i++)
                JoinLanesByIndices(strip, unchangingLanesStartRight + i, unchangingLanesEndRight-1);
            for(int i = 0; i < openingRightLanes; i++)
                JoinLanesByIndices(strip, unchangingLanesStartRight-1, unchangingLanesEndRight+i);
            return strip; // Return the created road strip
        }
        public static void GenerateOneToOneConnections(RoadStrip strip, int lstartIdx, int rstartIdx, int lendIdx, int rendIdx) {
            for (int i = lstartIdx; i < rstartIdx; i++) {
                for (int j = lendIdx; j < rendIdx; j++) {
                    var startLane = strip.StartNode.GetLaneEnd(i);
                    var endLane = strip.EndNode.GetLaneEnd(j);
                    LaneStrip laneStrip = new LaneStrip(strip, startLane, endLane); // Create a new lane strip connecting the start and end lanes
                    strip.AddLaneStrip(laneStrip); // Add the lane strip to the road strip
                }
            }
        }

        public static void JoinLanesByIndices(RoadStrip strip, int startIdx, int endIdx) {
            JoinLanesByIndices(strip, startIdx, endIdx, LaneSpec.Default);
        }
        public static void JoinLanesByIndices(RoadStrip strip, int startIdx, int endIdx, LaneSpec spec) {
            var startLane = strip.StartNode.GetLaneEnd(startIdx);
            var endLane = strip.EndNode.GetLaneEnd(endIdx);
            LaneStrip laneStrip = new LaneStrip(strip, startLane, endLane); // Create a new lane strip connecting the start and end lanes
            laneStrip.Spec = spec;
            strip.AddLaneStrip(laneStrip); // Add the lane strip to the road strip
        }
    }
}
