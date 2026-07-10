using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Tools {
    public class LaneMappings {
        public int LaneChangesLeft { get; private set; }
        public int LaneChangesRight { get; private set; }
        public ImmutableArray<LaneMapping> Mappings { get; private set; }
        public ImmutableArray<Lane> StartingLanes { get; private set; }
        public ImmutableArray<LaneNode> EndingLanes { get; private set; }
        public Range<float> Range { get; private set; }

        public LaneMappings(LaneCreationState laneCreationState, int laneChangesLeft, int laneChangesRight) {
            //Set fields up
            StartingLanes = laneCreationState.StartLane.GetRoadNode().Lanes.ToImmutableArray();
            LaneChangesLeft = laneChangesLeft;
            LaneChangesRight = laneChangesRight;

            //Find lanes to collapse/expand from
            int leftBound, rightBound = StartingLanes.Length - 1;
            for (leftBound = 0; leftBound <= rightBound; leftBound++) {
                var lane = StartingLanes[leftBound];
                if (lane.Spec.VehicleTypes.HasFlags(VehicleTypes.MotorVehicles)) break;
            }
            for (rightBound = StartingLanes.Length - 1; rightBound >= 0; rightBound--) {
                var lane = StartingLanes[rightBound];
                if (lane.Spec.VehicleTypes.HasFlags(VehicleTypes.MotorVehicles)) break;
            }
            if(leftBound > rightBound) {
                //No car lanes. Map one to one and return
                OneToOne();
                return;
            }

            int trafficLanes = rightBound - leftBound + 1;
            if (trafficLanes + int.Min(laneChangesLeft,0) + int.Min(laneChangesRight,0) <= 0) {
                //Tried to remove too many lanes. Map one to one and return
                OneToOne();
                return;
            }

            /* Lane mapping
             *        |  starting  |
             *        |  core  |
             * ^  |   |   ||   |
             * |  |   .   ||   .\
             * |   \  .   ||   . \
             * |    \ .   ||   .  \
             * |     \.   ||   .   |
             *        |   ||   |   |
             */

            //Count lane mappings
            var numberOfStrips = StartingLanes.Length;
            if (laneChangesLeft > 0) numberOfStrips += laneChangesLeft;
            if (laneChangesRight > 0) numberOfStrips += laneChangesRight;
            var laneMappings = new LaneMapping[numberOfStrips];
            int lmIndex = 0;

            //Count sidewalks
            int countSideLeft = leftBound;
            int countSideRight = StartingLanes.Length - rightBound - 1;

            //Find synthetic lane specs
            var roadSpecLeft = StartingLanes[countSideLeft+1].Spec;
            var roadSpecRight = StartingLanes[^(countSideRight+1)].Spec;
            var sidewalkOffsetLeft = laneChangesLeft * roadSpecLeft.Width;
            var sidewalkOffsetRight = laneChangesRight * roadSpecRight.Width;

            //Copy sidewalks
            int newCount = StartingLanes.Length + laneChangesLeft + laneChangesRight;
            var endingLanes = new LaneNode[newCount];
            for (int i = 0; i < countSideLeft; i++) {
                var existingSidewalk = StartingLanes[i];
                var newSidewalk = new LaneNode(existingSidewalk.Spec, existingSidewalk.MiddlePosition - sidewalkOffsetLeft);
                endingLanes[i] = newSidewalk;
                var lm = new LaneMapping(i, i, existingSidewalk.Spec, existingSidewalk.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }
            for(int i = 0; i < countSideRight; i++) {
                var existingSidewalk = StartingLanes[^i];
                var newSidewalk = new LaneNode(existingSidewalk.Spec, existingSidewalk.MiddlePosition + sidewalkOffsetRight);
                endingLanes[^i] = newSidewalk;
                var lm = new LaneMapping(StartingLanes.Length - i - 1, newCount - i - 1, existingSidewalk.Spec, existingSidewalk.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }

            //Constants
            var mergeFlagsMask = LaneFlags.MergeRight | LaneFlags.MergeLeft | LaneFlags.IsMerge;
            var mergePlain = LaneFlags.MergeLeft | LaneFlags.MergeRight;
            var mergeLeft = LaneFlags.IsMerge | LaneFlags.MergeLeft;
            var mergeRight = LaneFlags.IsMerge | LaneFlags.MergeRight;
            var exitLeft = LaneFlags.MergeLeft;
            var exitRight = LaneFlags.MergeRight;

            //Count core lanes
            int coreOffsetStart = countSideLeft;
            if (laneChangesLeft < 0) coreOffsetStart -= laneChangesLeft;
            int coreOffsetEnd = countSideRight;
            if (laneChangesLeft > 0) coreOffsetEnd += laneChangesLeft;
            int coreCount = 1 + rightBound - leftBound;
            if (laneChangesLeft < 0) coreCount += laneChangesLeft;
            if(laneChangesRight < 0) coreCount += laneChangesRight;

            //Make left/right expanded/merged lanes
            if (laneChangesLeft > 0) {
                //Expansion on the left
                for(int i = 0; i < laneChangesLeft; i++) {
                    int newIndex = i + leftBound;
                    int prevIndex = leftBound;
                    var spec = roadSpecLeft;
                    var newflags = (i == laneChangesLeft-1) ? exitLeft : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                    var lane = StartingLanes[prevIndex];
                    var lanenode = new LaneNode(lane.Spec, lane.MiddlePosition - (i + 1) * lane.Spec.Width);
                    endingLanes[newIndex] = lanenode;
                }
            } else {
                //Merge on the left
                for (int i = 0; i < -laneChangesLeft; i++) {
                    int newIndex = leftBound;
                    int prevIndex = i + leftBound;
                    var spec = roadSpecLeft;
                    var newflags = (i == -laneChangesLeft-1) ? mergeRight : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                }
            }
            if (laneChangesRight > 0) {
                //Expansion on the right
                for (int i = 0; i < laneChangesRight; i++) {
                    int newIndex = newCount - countSideRight - i - 1;
                    int prevIndex = rightBound;
                    var spec = roadSpecRight;
                    var newflags = (i == laneChangesRight-1) ? exitRight : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                    var lane = StartingLanes[prevIndex];
                    var lanenode = new LaneNode(lane.Spec, lane.MiddlePosition + (i + 1) * lane.Spec.Width);
                    endingLanes[newIndex] = lanenode;
                }
            } else {
                //Merge on the right
                for (int i = 0; i < -laneChangesRight; i++) {
                    int newIndex = rightBound + laneChangesRight + laneChangesLeft;
                    int prevIndex = StartingLanes.Length - countSideRight - i - 1;
                    var spec = roadSpecRight;
                    var newflags = (i == -laneChangesRight-1) ? mergeLeft : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                }
            }

            //Copy core lanes
            for (int i = 0; i < coreCount; i++) {
                int startIndex = coreOffsetStart + i;
                int endIndex = coreOffsetEnd + i;
                var startLane = StartingLanes[startIndex];
                var endLane = new LaneNode(startLane.Spec, startLane.MiddlePosition);
                endingLanes[endIndex] = endLane;
                var lm = new LaneMapping(startIndex, endIndex, startLane.Spec, startLane.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }

            //Calculate end bounds
            Range = new();
            foreach(var endLane in endingLanes) {
                Range = Range.Union(endLane.Bounds);
            }

            //Push results
            Debug.Assert(lmIndex == laneMappings.Length, "Not all lane mappings are populated");
            Debug.Assert(newCount > 0, "Returned with no output lanes");
            EndingLanes = endingLanes.ToImmutableArray();
            Mappings = laneMappings.ToImmutableArray();
        }
        [Conditional("DEBUG")]
        private void ValidateMappings(int startCount, int endCount, LaneMapping mapping) {
            Debug.Assert(mapping.StartIndex >= 0 && mapping.StartIndex < startCount, "Out of bounds start index");
            Debug.Assert(mapping.EndIndex >= 0 && mapping.EndIndex < endCount, "Out of bounds end index");
        }
        private void OneToOne() {
            int count = StartingLanes.Length;
            var endingLanes = new LaneNode[count];
            var mappings = new LaneMapping[count];
            for (int i = 0; i < count; i++) {
                var laneDef = StartingLanes[i];
                var newLaneDef = new LaneNode(laneDef.Spec, laneDef.MiddlePosition, Guid.NewGuid());
                var isReverse = IsReverseLaneHeuristic(laneDef);
                endingLanes[i] = newLaneDef;
                var lm = new LaneMapping(i, i, laneDef.Spec);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                mappings[i] = lm;
            }
            Mappings = mappings.ToImmutableArray();
            EndingLanes = endingLanes.ToImmutableArray();
            Range = new();
            foreach (var endLane in endingLanes) {
                Range = Range.Union(endLane.Bounds);
            }
        }
        public static bool IsReverseLaneHeuristic(Lane lane) {
            //Should the road go forward or backward?
            var (forwardCount, backwardCount) = lane.CountLaneDirections();

            var isBackPreferred = backwardCount > forwardCount;
            var isForwardPreferred = backwardCount < forwardCount;
            var isLaneLeft = lane.MiddlePosition < 0;

            var isBackwards = isBackPreferred || (!isForwardPreferred && isLaneLeft);
            return isBackwards;
        }
    }
}
