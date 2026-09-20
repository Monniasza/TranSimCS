using TranSimCS;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    /// <summary>
    /// Covers the real implementation of <c>HalfNodeLanesList.Insert</c>, which previously delegated to
    /// <c>Add</c> and so could only ever append at the outside.
    /// </summary>
    public class TestHalfNodeInsert {
        private static LaneSpec Spec(float width = 3f, VehicleTypes types = VehicleTypes.Car)
            => new(Colors.Gray, types, width, 50);

        private static RoadNode MakeNode(params float[] centers) {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            foreach (var center in centers) node.AddLane(new LaneNode(Spec(), center));
            return node;
        }

        /// <summary>
        /// Builds a detached half-lane to insert. <c>Insert</c> only reads the lane's spec and width, so
        /// the lane does not need to belong to a node yet.
        /// </summary>
        private static HalfLane MakeHalfLane(NodeEnd end, float width = 3f)
            => new Lane(new RoadNode("scratch", PositionEulerAngles.Zero), new LaneNode(Spec(width), 0f))
                .GetHalfLane(end);

        [Fact]
        public void InsertAtFrontIndexZeroPlacesLaneLeftmost() {
            var node = MakeNode(0f, 3f);
            var lanes = node.FrontHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Forward);
            lanes.Insert(0, newLane);

            Assert.Equal(3, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(0).Guid);
        }

        [Fact]
        public void InsertInTheMiddlePlacesLaneAtThatIndex() {
            var node = MakeNode(-3f, 0f, 3f);
            var lanes = node.FrontHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Forward);
            lanes.Insert(1, newLane);

            Assert.Equal(4, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(1).Guid);
        }

        [Fact]
        public void InsertAtCountAppendsRightmost() {
            var node = MakeNode(-3f, 0f);
            var lanes = node.FrontHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Forward);
            lanes.Insert(lanes.Count, newLane);

            Assert.Equal(3, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(2).Guid);
        }

        [Fact]
        public void InsertAtEveryIndexProducesThatIndex() {
            for (int target = 0; target <= 3; target++) {
                var node = MakeNode(-4.5f, -1.5f, 1.5f);
                var lanes = node.FrontHalf.GetLaneList();

                var newLane = MakeHalfLane(NodeEnd.Forward);
                lanes.Insert(target, newLane);

                Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(target).Guid);
            }
        }

        [Fact]
        public void InsertedLaneDoesNotOverlapItsNeighbours() {
            var node = MakeNode(-3f, 0f, 3f);
            var lanes = node.FrontHalf.GetLaneList();

            lanes.Insert(1, MakeHalfLane(NodeEnd.Forward));

            var bounds = node.FrontHalf.SortedLanes.Select(x => x.Bounds).ToArray();
            for (int i = 0; i + 1 < bounds.Length; i++)
                Assert.True(bounds[i].Max <= bounds[i + 1].Min,
                    $"Lane {i} (max {bounds[i].Max}) overlaps lane {i + 1} (min {bounds[i + 1].Min}).");
        }

        [Fact]
        public void InsertIntoATooNarrowGapPushesNeighboursApart() {
            //Two lanes touching at 0, leaving no room for a third.
            var node = MakeNode(-1.5f, 1.5f);
            var lanes = node.FrontHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Forward);
            lanes.Insert(1, newLane);

            Assert.Equal(3, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(1).Guid);

            var bounds = node.FrontHalf.SortedLanes.Select(x => x.Bounds).ToArray();
            for (int i = 0; i + 1 < bounds.Length; i++)
                Assert.True(bounds[i].Max <= bounds[i + 1].Min,
                    $"Lane {i} (max {bounds[i].Max}) overlaps lane {i + 1} (min {bounds[i + 1].Min}).");
        }

        [Fact]
        public void InsertIntoEmptyHalfNodeWorks() {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            var lanes = node.FrontHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Forward);
            lanes.Insert(0, newLane);

            Assert.Equal(1, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(0).Guid);
        }

        [Fact]
        public void InsertThroughRearHalfMirrorsThePosition() {
            var node = MakeNode(-3f, 0f, 3f);
            var rearLanes = node.RearHalf.GetLaneList();

            var newLane = MakeHalfLane(NodeEnd.Backward);
            rearLanes.Insert(1, newLane);

            Assert.Equal(4, node.Lanes.Count);
            Assert.Equal(newLane.Guid, node.RearHalf.GetLaneByIndex(1).Guid);

            //The rear half's index 1 is the RoadNode's index 2, and the position is mirrored: the rear
            //half's own left-to-right order runs opposite to the RoadNode's.
            var stored = node.SortedLanes[2];
            Assert.Equal(newLane.Guid, stored.Guid);
            Assert.Equal(-node.RearHalf.GetLaneByIndex(1).MiddlePosition, stored.MiddlePosition);
        }

        [Fact]
        public void InsertOutOfRangeThrows() {
            var node = MakeNode(0f);
            var lanes = node.FrontHalf.GetLaneList();
            var lane = MakeHalfLane(NodeEnd.Forward);

            Assert.Throws<ArgumentOutOfRangeException>(() => lanes.Insert(-1, lane));
            Assert.Throws<ArgumentOutOfRangeException>(() => lanes.Insert(2, lane));
        }

        [Fact]
        public void InsertNullThrows() {
            var node = MakeNode(0f);
            var lanes = node.FrontHalf.GetLaneList();
            Assert.Throws<ArgumentNullException>(() => lanes.Insert(0, null!));
        }

        [Fact]
        public void InsertedLaneIsVisibleFromBothHalves() {
            var node = MakeNode(-3f, 3f);
            var newLane = MakeHalfLane(NodeEnd.Forward);
            node.FrontHalf.GetLaneList().Insert(1, newLane);

            Assert.Equal(3, node.FrontHalf.LaneCount);
            Assert.Equal(3, node.RearHalf.LaneCount);
            Assert.Equal(newLane.Guid, node.FrontHalf.GetLaneByIndex(1).Guid);
            Assert.Equal(newLane.Guid, node.RearHalf.GetLaneByIndex(1).Guid);
        }
    }
}
