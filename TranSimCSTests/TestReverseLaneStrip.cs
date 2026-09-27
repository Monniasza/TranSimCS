using System.Linq;
using TranSimCS.Cars;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;

namespace TranSimCSTests {
    /// <summary>
    /// Tests that reversing a lane strip while cars are occupying it does not crash the simulation.
    /// <para>
    /// The crash was caused by three bugs working together:
    /// <list type="number">
    ///   <item><c>ReverseDirection</c> did not orphan the old path, leaving it <c>Active</c> with dead
    ///       attachment points.</item>
    ///   <item>The <c>_cars</c> set on <c>SplinePath</c> was never populated, so the path GC believed every
    ///       path was unoccupied and collected (deleted) orphaned paths that cars still referenced.</item>
    ///   <item><c>TrimUntilDead</c> returned <c>true</c> when the car's current path was deleted but did not
    ///       empty the route, so callers checking <c>RouteElementCount == 0</c> proceeded to call
    ///       <c>GetSpline()</c> on the deleted path and threw.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TestReverseLaneStrip {
        // --- Fix 1: ReverseDirection orphans the old path ----------------------------------------

        /// <summary>
        /// Reversing a lane strip orphans the old strip's path rather than leaving it active, so that no
        /// new traffic is routed onto the reversed (wrong-way) path while existing traffic drains.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReversingALaneStripOrphansTheOldPath(bool reverse) {
            var world = TestWorlds.BuildStraightRoadWorld();
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            Assert.Equal(PathState.Active, path.CurrentState);

            var newStrip = strip.ReverseDirection();

            Assert.Equal(PathState.Orphaned, path.CurrentState);
            Assert.Null(path.Claimant);
            Assert.Same(path, world.Paths.FindPath(path.Guid));
            Assert.Equal(PathState.Active, newStrip.Path!.CurrentState);
        }

        // --- Fix 2: GC does not collect paths that cars are still on ------------------------------

        /// <summary>
        /// An orphaned path that a car is still on is not collected by the GC, because the car index
        /// (<c>_cars</c>) is now populated during the world update.
        /// </summary>
        [Fact]
        public void OrphanedPathWithCarIsNotCollectedByGC() {
            var world = TestWorlds.BuildStraightRoadWorld();
            var strip = TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);
            var path = strip.Path!;

            world.Update(0.1f);
            Assert.Same(path, car.GetRouteElement(0).road);
            Assert.True(path.Cars.Count > 0, "The car should be indexed on the path after a world update");

            path.Displace(null);
            Assert.Equal(PathState.Orphaned, path.CurrentState);

            world.Paths.RunGC();

            Assert.NotEqual(PathState.Deleted, path.CurrentState);
            Assert.Same(path, world.Paths.FindPath(path.Guid));
        }

        // --- Fix 3: TrimUntilDead empties the route when the car is dead --------------------------

        /// <summary>
        /// A car whose route path has been deleted from under it is removed gracefully on the next update,
        /// rather than crashing with "Using a Deleted path".
        /// </summary>
        [Fact]
        public void CarIsRemovedGracefullyWhenRoutePathIsDeleted() {
            var world = TestWorlds.BuildStraightRoadWorld();
            var strip = TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);
            var path = strip.Path!;

            car.Update(0.1f);
            Assert.Same(path, car.GetRouteElement(0).road);

            path.Displace(null);
            path._cars.Clear();
            path._carsOnStrip.Clear();
            world.Paths.RunGC();
            Assert.Equal(PathState.Deleted, path.CurrentState);

            var exception = Record.Exception(() => car.Update(0.1f));
            Assert.Null(exception);
            Assert.Equal(0, car.RouteElementCount);
        }

        // --- Integration: reversing a strip under a car does not crash ----------------------------

        /// <summary>
        /// Reversing a lane strip while a car is on it does not crash the world update, and the old path
        /// is orphaned rather than deleted so the car can drain off it.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReversingALaneStripUnderACarDoesNotCrash(bool reverse) {
            var world = TestWorlds.BuildStraightRoadWorld();
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);
            var path = strip.Path!;

            world.Update(0.1f);
            Assert.Same(path, car.GetRouteElement(0).road);

            strip.ReverseDirection();

            var exception = Record.Exception(() => world.Update(0.1f));
            Assert.Null(exception);

            Assert.NotEqual(PathState.Deleted, path.CurrentState);
            Assert.Same(path, world.Paths.FindPath(path.Guid));
        }

        /// <summary>
        /// Reversing a lane strip under a car and then running many world updates does not crash, covering
        /// the multi-frame window in which the car drains off the orphaned path and the path is eventually
        /// collected.
        /// </summary>
        [Fact]
        public void WorldUpdateHandlesReversedStripUnderACarAcrossMultipleFrames() {
            var world = TestWorlds.BuildStraightRoadWorld();
            var strip = TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);

            world.Update(0.1f);

            strip.ReverseDirection();

            for(int i = 0; i < 20; i++) {
                var exception = Record.Exception(() => world.Update(0.1f));
                Assert.Null(exception);
            }
        }

        /// <summary>
        /// Reversing every lane strip on a road (the RMB action in <see cref="TranSimCS.Mode.ModeReverse"/>)
        /// while cars are on it does not crash the world update.
        /// </summary>
        [Fact]
        public void ReversingAllLanesOfARoadUnderCarsDoesNotCrash() {
            var world = TestWorlds.BuildStraightRoadWorld();
            var forward = TestWorlds.ForwardStrip(world);
            var reverse = TestWorlds.ReverseStrip(world);
            Car.LaunchCar(world, forward);
            Car.LaunchCar(world, reverse);

            world.Update(0.1f);

            var road = TestWorlds.SingleRoad(world);
            foreach(var lane in road.Lanes.ToArray())
                lane.ReverseDirection();

            for(int i = 0; i < 20; i++) {
                var exception = Record.Exception(() => world.Update(0.1f));
                Assert.Null(exception);
            }
        }
    }
}
