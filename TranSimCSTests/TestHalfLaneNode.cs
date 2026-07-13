using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Assert.Same(leftLane.FrontHalf, frontHalf.GetLaneByIndex(0));
            Assert.Same(rightLane.FrontHalf, frontHalf.GetLaneByIndex(1));
            var rearHalf = node.RearHalf;
            Assert.Same(rightLane.RearHalf, rearHalf.GetLaneByIndex(0));
            Assert.Same(leftLane.RearHalf, rearHalf.GetLaneByIndex(1));
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

        [Fact]
        public void ChangingFrontDefinitionUpdatesRear() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            var lane = node.AddLane(new(LaneSpec.Default, 1));

            lane.FrontHalf.Definition = new LaneDefinition(5, LaneSpec.Default);

            Assert.Equal(-5, lane.RearHalf.Definition.CenterPosition);
        }

        [Fact]
        public void TestAddThroughRearMirrors() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            var rearNode = node.RearHalf;
            rearNode.AddLane(new(LaneSpec.Default, 3));
            var frontNode = node.FrontHalf;
            Assert.Equal(-3, frontNode.GetLaneByIndex(0).MiddlePosition);
        }

        [Fact]
        public void TestRemoveThroughHalfNode() {
            var node = new RoadNode("roadNode", PositionEulerAngles.Zero);
            var leftLane = node.AddLane(new(LaneSpec.Default, -1));
            var middleLane = node.AddLane(new(LaneSpec.Default, 0));
            var rightLane = node.AddLane(new(LaneSpec.Default, 1));

            var frontHalf = node.FrontHalf;
            var rearHalf = node.RearHalf;
            Assert.Throws<InvalidOperationException>(() => frontHalf.Delete(leftLane.RearHalf));
            Assert.Throws<InvalidOperationException>(() => rearHalf.Delete(rightLane.FrontHalf));
            frontHalf.Delete(leftLane.FrontHalf);
            rearHalf.Delete(middleLane.RearHalf);

            Assert.Single(node.Lanes);
            Assert.Equal(1, frontHalf.LaneCount);
            Assert.Equal(1, rearHalf.LaneCount);
            Assert.Same(rightLane.FrontHalf, frontHalf.GetLaneByIndex(0));
        }

        [Fact]
        public void TestLaneIndexIsReversedOnRearButNotFront() {
            var node = new RoadNode("roadNode", PositionEulerAngles.Zero);
            var leftLane = node.AddLane(new(LaneSpec.Default, -1));
            var rightLane = node.AddLane(new(LaneSpec.Default, 1));

            Assert.Equal(0, leftLane.FrontHalf.Index);
            Assert.Equal(1, rightLane.FrontHalf.Index);
            Assert.Equal(1, leftLane.RearHalf.Index);
            Assert.Equal(0, rightLane.RearHalf.Index);
        }

        [Fact]
        public void TestLaneEventsAreForwardedOnBothFrontAndRear() {
            HalfNode? halfNodeFromFrontAdd = null, halfNodeFromRearAdd = null, halfNodeFromFrontRemove = null, halfNodeFromRearRemove = null;
            HalfLane halfLaneFromFrontAdd = null, halfLaneFromRearAdd = null, halfLaneFromFrontRemove = null, halfLaneFromRearRemove = null;

            var node = new RoadNode("roadNode", PositionEulerAngles.Zero);
            node.FrontHalf.OnLaneAdded += (hn, hl) => {
                halfNodeFromFrontAdd = hn;
                halfLaneFromFrontAdd = hl;
            };
            node.RearHalf.OnLaneAdded += (hn, hl) => {
                halfNodeFromRearAdd = hn;
                halfLaneFromRearAdd = hl;
            };
            node.FrontHalf.OnLaneRemoved += (hn, hl) => {
                halfNodeFromFrontRemove = hn;
                halfLaneFromFrontRemove = hl;
            };
            node.RearHalf.OnLaneRemoved += (hn, hl) => {
                halfNodeFromRearRemove = hn;
                halfLaneFromRearRemove = hl;
            };

            var lane = node.AddLane(new(LaneSpec.Default, 1));
            node.RemoveLane(lane);

            Assert.Same(node.FrontHalf, halfNodeFromFrontAdd);
            Assert.Same(node.FrontHalf, halfNodeFromFrontRemove);
            Assert.Same(node.RearHalf, halfNodeFromRearAdd);
            Assert.Same(node.RearHalf, halfNodeFromRearRemove);
            Assert.Same(lane.FrontHalf, halfLaneFromFrontAdd);
            Assert.Same(lane.FrontHalf, halfLaneFromFrontRemove);
            Assert.Same(lane.RearHalf, halfLaneFromRearAdd);
            Assert.Same(lane.RearHalf, halfLaneFromRearRemove);
        }
    }
}
