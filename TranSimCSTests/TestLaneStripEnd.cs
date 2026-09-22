using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCSTests {
    public class TestLaneStripEnd {
        [Fact]
        public void TestDeleteLaneStripEnds() {
            RoadNode roadNode = new RoadNode("test", default);
            Generator.GenerateLanes(2, roadNode);
            var halfLaneA = roadNode.SortedLanes[0].FrontHalf;
            var halfLaneB = roadNode.SortedLanes[1].FrontHalf;

            var testStrip = new LaneStrip(halfLaneA, halfLaneB);

            var half = new LaneStripEnd(testStrip, SegmentHalf.Start);

            var set = new HashSet<LaneStripEnd>();

            set.Add(half);
            Assert.Contains(half, set);

            set.Remove(half);
            Assert.DoesNotContain(half, set);
        }
    }
}
