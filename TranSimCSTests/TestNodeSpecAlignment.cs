using System;
using System.Collections.Generic;
using System.Linq;
using TranSimCS;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet.RoadConstruction;
using Xunit;

namespace TranSimCSTests {
    public class TestNodeSpecAlignment {
        private static LaneNode Car(float width = 3.5f) =>
            new(new LaneSpec(Colors.Gray, VehicleTypes.MotorVehicles, width), 0, Guid.NewGuid());
        private static LaneNode Ped() =>
            new(new LaneSpec(Colors.LightGray, VehicleTypes.Pedestrian, 1.5f, 16, LaneFlags.Sidewalk), 0, Guid.NewGuid());
        private static LaneNode Tram() =>
            new(new LaneSpec(Colors.Green, VehicleTypes.LRT, 3), 0, Guid.NewGuid());

        /// <summary>Positions the lanes left to right at their packed positions.</summary>
        private static List<LaneNode> Pack(params LaneNode[] lanes) {
            var result = new List<LaneNode>(lanes.Length);
            var total = lanes.Sum(l => l.LaneSpec.Width);
            var x = -total / 2;
            foreach(var lane in lanes) {
                result.Add(new LaneNode(lane.LaneSpec, x + lane.LaneSpec.Width / 2, lane.ID));
                x += lane.LaneSpec.Width;
            }
            return result;
        }

        [Fact]
        public void IdenticalSpecsMapOneToOne() {
            var start = Pack(Car(), Car(), Car());
            var end = Pack(Car(), Car(), Car());
            var steps = NodeSpecAlignment.Align(start, end);
            Assert.Equal(3, steps.Count);
            Assert.All(steps, step => Assert.Equal(LaneAlignmentKind.Straight, step.Kind));
            for(int i = 0; i < 3; i++)
                Assert.Equal((i, i), (steps[i].StartIndex, steps[i].EndIndex));
        }

        [Fact]
        public void OuterLaneDropsMergeInsteadOfTerminating() {
            var start = Pack(Car(), Car(), Car());
            var end = Pack(Car(), Car());
            var steps = NodeSpecAlignment.Align(start, end);
            Assert.Equal(2, steps.Count);
            Assert.Equal(1, steps.Count(s => s.Kind == LaneAlignmentKind.Straight));
            Assert.Equal(1, steps.Count(s => s.Kind == LaneAlignmentKind.Merge));
            Assert.DoesNotContain(steps, s => s.Kind == LaneAlignmentKind.Terminated);
        }

        [Fact]
        public void InteriorMergeBetweenSidewalks() {
            var start = Pack(Ped(), Car(), Car(), Car(), Ped());
            var end = Pack(Ped(), Car(), Car(), Ped());
            var steps = NodeSpecAlignment.Align(start, end);
            Assert.Equal(1, steps.Count(s => s.Kind == LaneAlignmentKind.Merge));
            Assert.Equal(3, steps.Count(s => s.Kind == LaneAlignmentKind.Straight));
            Assert.DoesNotContain(steps, s => s.Kind == LaneAlignmentKind.Terminated || s.Kind == LaneAlignmentKind.Spawned);
            //Sidewalks stay on the outside
            Assert.Equal((0, 0), (steps[0].StartIndex, steps[0].EndIndex));
            Assert.Equal((4, 3), (steps[^1].StartIndex, steps[^1].EndIndex));
        }

        [Fact]
        public void LaneExpandsIntoTwo() {
            var start = Pack(Car(), Car());
            var end = Pack(Car(), Car(), Car());
            var steps = NodeSpecAlignment.Align(start, end);
            Assert.Equal(2, steps.Count);
            Assert.Equal(1, steps.Count(s => s.Kind == LaneAlignmentKind.Expand));
            Assert.Equal(1, steps.Count(s => s.Kind == LaneAlignmentKind.Straight));
        }

        [Fact]
        public void IncompatibleLanesTerminateAndSpawn() {
            var start = Pack(Car());
            var end = Pack(Tram());
            var steps = NodeSpecAlignment.Align(start, end);
            Assert.Contains(steps, s => s.Kind == LaneAlignmentKind.Terminated);
            Assert.Contains(steps, s => s.Kind == LaneAlignmentKind.Spawned);
            Assert.DoesNotContain(steps, s => s.Kind is LaneAlignmentKind.Straight or LaneAlignmentKind.Merge or LaneAlignmentKind.Expand);
        }

        [Fact]
        public void AlignmentPreservesOrderWithoutCrossings() {
            var start = Pack(Ped(), Car(), Car(), Car(), Car(), Ped());
            var end = Pack(Ped(), Car(), Car(), Ped());
            var steps = NodeSpecAlignment.Align(start, end);
            //Straight mappings must be monotonic on both sides
            var straights = steps.Where(s => s.Kind == LaneAlignmentKind.Straight).ToList();
            for(int i = 1; i < straights.Count; i++) {
                Assert.True(straights[i - 1].StartIndex < straights[i].StartIndex);
                Assert.True(straights[i - 1].EndIndex < straights[i].EndIndex);
            }
        }

        [Fact]
        public void MergeProducesTwoMappingsIntoSameEnd() {
            var start = Pack(Car(), Car(), Car());
            var end = Pack(Car(), Car());
            var steps = NodeSpecAlignment.Align(start, end);
            var mappings = NodeSpecAlignment.ToLaneMappings(steps, start, end);
            Assert.Equal(3, mappings.Count); //1 straight + 2 merge strips

            var mergeStep = steps.First(s => s.Kind == LaneAlignmentKind.Merge);
            var mergeStrips = mappings.Where(m => m.StartIndex == mergeStep.StartIndex || m.StartIndex == mergeStep.StartIndex2);
            Assert.Equal(2, mergeStrips.Count());
            //Both merge strips target the same end lane
            Assert.All(mergeStrips, m => Assert.Equal(mergeStep.EndIndex, m.EndIndex));
            //The outer strip carries the merge flag
            Assert.Single(mergeStrips, m => m.LaneSpec.Flags.HasFlags(LaneFlags.IsMerge));
        }

        [Fact]
        public void ExpandProducesTwoMappingsFromSameStart() {
            var start = Pack(Car(), Car());
            var end = Pack(Car(), Car(), Car());
            var steps = NodeSpecAlignment.Align(start, end);
            var mappings = NodeSpecAlignment.ToLaneMappings(steps, start, end);
            Assert.Equal(3, mappings.Count); //1 straight + 2 expand strips

            var expandStep = steps.First(s => s.Kind == LaneAlignmentKind.Expand);
            var expandMappings = mappings.Where(m => m.StartIndex == expandStep.StartIndex).ToList();
            Assert.Equal(2, expandMappings.Count);
            //Both expansions target the two end lanes of the step
            Assert.All(expandMappings, m => Assert.True(m.EndIndex == expandStep.EndIndex || m.EndIndex == expandStep.EndIndex2));
            //One of the expand strips is the passthrough, the other diverges sideways
            Assert.Single(expandMappings, m => m.LaneSpec.Flags.HasFlags(LaneFlags.MergeRight));
        }

        [Fact]
        public void TerminateAndSpawnProduceNoMappings() {
            var start = Pack(Car());
            var end = Pack(Tram());
            var steps = NodeSpecAlignment.Align(start, end);
            var mappings = NodeSpecAlignment.ToLaneMappings(steps, start, end);
            Assert.Empty(mappings);
        }
    }
}
