using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    public class TestHalfLaneNode {
        [Fact]
        public void TestLaneOrderIsReversedInRear() {
            var node = new RoadNode("roadNode", PositionEulerAngles.Zero);
            var leftLane = node.AddLane(new(LaneSpec.Default, -1));
            var rightLane = node.AddLane(new(LaneSpec.Default, 1));

            var frontHalf = node.FrontHalf;
            Assert.Equal(leftLane.FrontHalf, frontHalf.GetLaneByIndex(0));
            Assert.Equal(rightLane.FrontHalf, frontHalf.GetLaneByIndex(1));
            var rearHalf = node.RearHalf;
            Assert.Equal(rightLane.RearHalf, rearHalf.GetLaneByIndex(0));
            Assert.Equal(leftLane.RearHalf, rearHalf.GetLaneByIndex(1));
        }

        [Fact]
        public void TestBoundsAreMirrored() {
            var node = new RoadNode("roadNode", PositionEulerAngles.Zero);
            var leftLane = node.AddLane(new(LaneSpec.Default, -1));
            var rightLane = node.AddLane(new(LaneSpec.Default, 2));

            var frontRange = node.FrontHalf.Bounds;
            Assert.Equal(new Range<float>(-2.5f, 3.5f), frontRange);
            var rearRange = node.RearHalf.Bounds;
            Assert.Equal(new Range<float>(-3.5f, 2.5f), rearRange);
        }

        [Fact]
        public void TestReferenceFramesAreMirrored() {
            var node = new RoadNode("roadNode", new(new(1, 2, 3), 987654321, 1, 0.5f));
            var frontFrame = node.FrontHalf.Cache.ReferenceFrame;
            var rearFrame = node.RearHalf.Cache.ReferenceFrame;
            Assert.Equal(frontFrame.O, rearFrame.O);
            Assert.Equal(frontFrame.Y, rearFrame.Y);
            Assert.Equal(frontFrame.X, - rearFrame.X);
            Assert.Equal(frontFrame.Z, - rearFrame.Z);
        }
    }
}
