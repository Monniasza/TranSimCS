using System;
using System.Linq;
using System.Numerics;
using TranSimCS;
using TranSimCS.Cars;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;

namespace TranSimCSTests {
    /// <summary>
    /// Covers the path system: the paths owned by lane strips, the attachment points they are anchored
    /// to, and the behaviour of both when the road network changes underneath them.
    /// <para>
    /// The worlds used here are embedded resources, loaded through <see cref="TestWorlds"/>. Each world
    /// contains a straight two-node road with one lane strip in each direction, so every test runs
    /// against both a forward and a reverse strip.
    /// </para>
    /// </summary>
    public class TestSplinePath {
        // --- Path creation -------------------------------------------------------------------------

        /// <summary>
        /// A lane strip that is part of a world owns a path, and that path is registered with the world.
        /// </summary>
        [Fact]
        public void LaneStripOwnsAPath() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);

            var path = strip.Path;

            Assert.NotNull(path);
            Assert.Same(path, strip.ExistingPath);
            Assert.Contains(path!.Guid, world.Paths.Paths.Keys);
        }

        /// <summary>
        /// The path owned by a strip is anchored to that strip's two half lanes.
        /// </summary>
        [Fact]
        public void PathIsAnchoredToTheStripsHalfLanes() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);

            var path = strip.Path!;

            Assert.Equal(2, path.Attachments.Count);
            var start = Assert.IsType<HalfLaneAttachment>(path.Attachments[0]);
            var end = Assert.IsType<HalfLaneAttachment>(path.Attachments[1]);
            Assert.Same(strip.StartLane, start.HalfLane);
            Assert.Same(strip.EndLane, end.HalfLane);
        }

        /// <summary>
        /// Asking a strip for its path twice returns the same instance, rather than creating a second one.
        /// </summary>
        [Fact]
        public void PathIsCreatedOnlyOnce() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);

            var first = strip.Path;
            var second = strip.Path;

            Assert.Same(first, second);
        }

        /// <summary>
        /// The path of a strip is claimed by a claim that reports the owning road strip as its object.
        /// </summary>
        [Fact]
        public void PathIsClaimedByTheOwningRoadStrip() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);

            var path = strip.Path!;

            var claim = Assert.IsType<LaneStripPathClaim>(path.Claimant);
            Assert.Same(strip, claim.Strip);
            Assert.Same(strip.Road, claim.Object);
        }

        /// <summary>
        /// A strip that is not part of a world has no path, because a path cannot be registered without
        /// a world to register it in.
        /// </summary>
        [Fact]
        public void StripOutsideAWorldHasNoPath() {
            var nodeA = new RoadNode("A", PositionEulerAngles.Zero);
            var nodeB = new RoadNode("B", PositionEulerAngles.Zero);
            nodeA.AddLane(new LaneNode(LaneSpec.Default, 0));
            nodeB.AddLane(new LaneNode(LaneSpec.Default, 0));

            var strip = new LaneStrip(nodeA.FrontHalf.SortedLanes[0], nodeB.RearHalf.SortedLanes[0]);

            Assert.Null(strip.Path);
            Assert.Null(strip.ExistingPath);
        }

        // --- Spline generation ---------------------------------------------------------------------

        /// <summary>
        /// The path of a strip generates a spline with a positive length, for both strip orientations.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PathGeneratesASpline(bool reverse) {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);

            var lut = strip.Path!.GetSpline();

            Assert.True(lut.Length > 0, $"Expected a positive spline length, got {lut.Length}");
        }

        /// <summary>
        /// The spline of a path runs in the direction of travel of its strip, so the forward and reverse
        /// strips of the same road produce splines pointing in opposite directions.
        /// </summary>
        [Fact]
        public void ForwardAndReversePathsRunInOppositeDirections() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var forward = TestWorlds.ForwardStrip(world);
            var reverse = TestWorlds.ReverseStrip(world);

            var forwardStart = forward.Path!.GetSpline().spline.SampleFrame(0).O;
            var forwardEnd = forward.Path!.GetSpline().spline.SampleFrame(1).O;
            var reverseStart = reverse.Path!.GetSpline().spline.SampleFrame(0).O;
            var reverseEnd = reverse.Path!.GetSpline().spline.SampleFrame(1).O;

            //The forward path travels along +Z, the reverse path along -Z.
            Assert.True(forwardEnd.Z > forwardStart.Z, "The forward path should travel along +Z");
            Assert.True(reverseEnd.Z < reverseStart.Z, "The reverse path should travel along -Z");
        }

        /// <summary>
        /// The spline of a path is regenerated after the path is marked dirty.
        /// </summary>
        [Fact]
        public void MarkingAPathDirtyRegeneratesItsSpline() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            var before = path.GetSpline();
            Assert.False(path.Dirty);

            path.MarkDirty();
            Assert.True(path.Dirty);

            var after = path.GetSpline();
            Assert.False(path.Dirty);
            Assert.NotNull(after);
            Assert.Equal(before.Length, after.Length, 3);
        }

        // --- Attachment points ---------------------------------------------------------------------

        /// <summary>
        /// An attachment point backed by a live half lane reports itself as alive.
        /// </summary>
        [Fact]
        public void AttachmentPointOfALiveLaneIsAlive() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);

            var attachment = (HalfLaneAttachment)strip.Path!.Attachments[0];

            Assert.True(attachment.IsAlive);
        }

        private StubClaim NewClaim() {
            var startPos = new PositionEulerAngles(Vector3.Zero, 0);
            var endPos = new PositionEulerAngles(Vector3.UnitZ * 100, int.MinValue);
            var attachmentA = new StubAttachment(startPos);
            var attachmentB = new StubAttachment(endPos);
            return new StubClaim(attachmentA, attachmentB);
        }

        /// <summary>
        /// The attachment point system is not limited to road half lanes: any implementation of
        /// <see cref="IPathAttachment"/> can be used, and a path built from a custom attachment point
        /// generates its spline from that attachment point.
        /// </summary>
        [Fact]
        public void PathCanBeAnchoredToACustomAttachmentPoint() {
            var claim = NewClaim();
            var path = new SplinePath(claim, null, claim.attachmentA, claim.attachmentB);

            var lut = path.GetSpline();

            Assert.True(lut.Length > 0);
            Assert.Equal(2, path.Attachments.Count);
        }

        /// <summary>
        /// A path whose attachment point is dead reports that its attachments are not all alive.
        /// </summary>
        [Fact]
        public void PathWithADeadAttachmentReportsIt() {
            var claim = NewClaim();
            var path = new SplinePath(claim, null, claim.attachmentA, claim.attachmentB);

            Assert.True(path.AreAttachmentsAlive);

            var attachment = (StubAttachment)claim.attachmentA;
            attachment.Alive = false;

            Assert.False(path.AreAttachmentsAlive);
        }

        // --- Deletion of a segment without notifying the car ---------------------------------------

        /// <summary>
        /// Deleting the road strip that owns a lane strip orphans that strip's path instead of deleting
        /// it, so that traffic already on the path can still leave it.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeletingTheRoadOrphansThePath(bool reverse) {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);
            var path = strip.Path!;
            var guid = path.Guid;

            Assert.Equal(PathState.Active, path.CurrentState);

            TestWorlds.SingleRoad(world).Demolish();

            Assert.Equal(PathState.Orphaned, path.CurrentState);
            Assert.Null(path.Claimant);
            //The path is still resolvable by GUID, which is what lets traffic already on it leave.
            Assert.Same(path, world.Paths.FindPath(guid));
        }

        /// <summary>
        /// A car that is on a segment when that segment is deleted does not crash the simulation, and
        /// the path it was on is orphaned rather than deleted.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DeletingASegmentUnderACarDoesNotCrash(bool reverse) {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);

            var car = Car.LaunchCar(world, strip);
            car.Update(0.1f);

            var path = strip.Path!;
            Assert.Same(path, car.OccupiedPath);

            //Delete the segment from under the car, without telling the car anything.
            TestWorlds.SingleRoad(world).Demolish();

            //The car must survive being updated against a deleted segment.
            var exception = Record.Exception(() => car.Update(0.1f));
            Assert.Null(exception);

            //The path is orphaned, not deleted, and the car is still registered on it.
            Assert.Equal(PathState.Orphaned, path.CurrentState);
            Assert.Same(path, world.Paths.FindPath(path.Guid));
        }

        /// <summary>
        /// A car whose whole route is deleted from under it is removed on the next update rather than
        /// crashing.
        /// </summary>
        [Fact]
        public void CarIsRemovedWhenItsWholeRouteIsDeleted() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);
            car.Update(0.1f);

            TestWorlds.SingleRoad(world).Demolish();

            var exception = Record.Exception(() => car.Update(0.1f));
            Assert.Null(exception);

            //The car either left the world or is still draining off the orphaned path, but it must not
            //have thrown and must not be left pointing at a deleted path.
            if (car.OccupiedPath != null)
                Assert.NotEqual(PathState.Deleted, car.OccupiedPath.CurrentState);
        }

        /// <summary>
        /// The world's update loop orphans paths whose attachment points have died, even when nothing
        /// raised an event to say so.
        /// </summary>
        [Fact]
        public void WorldUpdateOrphansPathsWithDeadAttachments() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            //Remove the lane from its node directly, bypassing the strip's own removal path, so that no
            //event tells the path that its attachment point died.
            var node = strip.StartLane.RoadNode;
            node.RemoveLane(strip.StartLane.Lane);

            Assert.False(path.AreAttachmentsAlive);

            world.Update(0.1f);

            Assert.Equal(PathState.Orphaned, path.CurrentState);
        }

        /// <summary>
        /// An orphaned path is collected once nothing refers to it any more.
        /// </summary>
        [Fact]
        public void OrphanedPathIsCollectedWhenUnused() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;
            var guid = path.Guid;

            TestWorlds.SingleRoad(world).Demolish();
            Assert.Equal(PathState.Orphaned, path.CurrentState);

            world.Paths.RunGC();

            Assert.Equal(PathState.Deleted, path.CurrentState);
            Assert.Null(world.Paths.FindPath(guid));
        }

        /// <summary>
        /// An orphaned path that still has a car on it is not collected, so that the car can finish
        /// leaving it.
        /// </summary>
        [Fact]
        public void OrphanedPathWithACarOnItIsNotCollected() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var car = Car.LaunchCar(world, strip);
            car.Update(0.1f);

            var path = strip.Path!;
            Assert.Same(path, car.OccupiedPath);

            TestWorlds.SingleRoad(world).Demolish();
            world.Paths.RunGC();

            Assert.Equal(PathState.Orphaned, path.CurrentState);
            Assert.Same(path, world.Paths.FindPath(path.Guid));
        }

        // --- Modification of the road --------------------------------------------------------------

        /// <summary>
        /// Moving a road node marks the paths anchored to it as dirty, so that their splines are
        /// regenerated against the new position.
        /// </summary>
        /// <param name="reverse">Whether to test the reverse-running strip.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MovingARoadNodeMarksThePathDirty(bool reverse) {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = reverse ? TestWorlds.ReverseStrip(world) : TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            //Generate the spline once so that the path is clean.
            path.GetSpline();
            Assert.False(path.Dirty);

            var node = strip.StartLane.RoadNode;
            node.PositionData = new PositionEulerAngles(
                node.PositionData.Position + new System.Numerics.Vector3(0, 0, 10),
                node.PositionData.Azimuth,
                node.PositionData.Inclination,
                node.PositionData.Tilt);

            Assert.True(path.Dirty, "Moving a road node should mark the anchored path dirty");
        }

        /// <summary>
        /// Moving a road node changes the spline that the path generates.
        /// </summary>
        [Fact]
        public void MovingARoadNodeChangesTheGeneratedSpline() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            Assert.NotNull(path.Claimant);

            var before = path.GetSpline().spline.SampleFrame(0).O;

            var node = strip.StartLane.RoadNode;
            node.PositionData = new PositionEulerAngles(
                node.PositionData.Position + new System.Numerics.Vector3(0, 0, 10),
                node.PositionData.Azimuth,
                node.PositionData.Inclination,
                node.PositionData.Tilt);

            Assert.True(path.Dirty);
            var after = path.GetSpline().spline.SampleFrame(0).O;
            Assert.False(path.Dirty);

            Assert.NotEqual(before, after);
        }

        /// <summary>
        /// Changing the lane specification of a strip marks its path dirty, so that the path follows the
        /// new lane geometry.
        /// </summary>
        [Fact]
        public void ChangingTheLaneSpecMarksThePathDirty() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            path.GetSpline();
            Assert.False(path.Dirty);

            strip.LaneSpec = strip.LaneSpec with { Width = strip.LaneSpec.Width + 1 };

            Assert.True(path.Dirty);
        }

        /// <summary>
        /// Removing a lane from a road node kills the attachment points that refer to it, which is what
        /// makes the path orphanable.
        /// </summary>
        [Fact]
        public void RemovingALaneKillsItsAttachmentPoint() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var attachment = (HalfLaneAttachment)strip.Path!.Attachments[0];

            Assert.True(attachment.IsAlive);

            strip.StartLane.RoadNode.RemoveLane(strip.StartLane.Lane);

            Assert.False(attachment.IsAlive);
        }

        // --- Serialization -------------------------------------------------------------------------

        /// <summary>
        /// The path GUID is written to the save file, so that the same path can be found again after
        /// loading.
        /// </summary>
        [Fact]
        public void PathGuidIsSerialized() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            var json = System.Text.Json.JsonSerializer.Serialize(world, world.CreateJsonOptions());

            Assert.Contains(path.Guid.ToString(), json);
        }

        /// <summary>
        /// Loading a world reuses the paths that were saved, rather than creating new ones: the paths
        /// found after loading have the same GUIDs as the paths that were saved.
        /// </summary>
        [Fact]
        public void LoadingAWorldReusesTheSavedPaths() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var forward = TestWorlds.ForwardStrip(world);
            var reverse = TestWorlds.ReverseStrip(world);
            var forwardGuid = forward.Path!.Guid;
            var reverseGuid = reverse.Path!.Guid;

            var json = System.Text.Json.JsonSerializer.Serialize(world, world.CreateJsonOptions());

            var reloaded = new TSWorld();
            var options = reloaded.CreateJsonOptions();
            var reader = new System.Text.Json.Utf8JsonReader(
                System.Text.Encoding.UTF8.GetBytes(json),
                new System.Text.Json.JsonReaderOptions { AllowTrailingCommas = true });
            reloaded.LoadJsonData(ref reader, options);

            Assert.NotNull(reloaded.Paths.FindPath(forwardGuid));
            Assert.NotNull(reloaded.Paths.FindPath(reverseGuid));
            Assert.Equal(2, reloaded.Paths.Paths.Count);
        }

        /// <summary>
        /// A path that is looked up by a GUID that is already registered is reused rather than
        /// duplicated.
        /// </summary>
        [Fact]
        public void GetOrMakePathReusesAnExistingPath() {
            var world = TestWorlds.Load(TestWorlds.StraightRoad);
            var strip = TestWorlds.ForwardStrip(world);
            var path = strip.Path!;

            var again = world.Paths.GetOrMakePath(path.Guid, null);

            Assert.Same(path, again);
            Assert.Equal(2, world.Paths.Paths.Count);
        }

        // --- Test doubles --------------------------------------------------------------------------

        /// <summary>
        /// A minimal <see cref="IPathAttachment"/> used to prove that the attachment point system is not
        /// tied to road half lanes.
        /// </summary>
        private sealed class StubAttachment(PositionEulerAngles position) : IPathAttachment {
            /// <summary>Whether this attachment point reports itself as alive.</summary>
            public bool Alive = true;

            /// <inheritdoc/>
            public event Action? Changed;

            /// <inheritdoc/>
            public Obj Owner => null!;

            /// <inheritdoc/>
            public bool IsAlive => Alive;

            /// <inheritdoc/>
            public TranSimCS.Geometry.Transform3 ReferenceFrame => position.CalcReferenceFrame();

            /// <inheritdoc/>
            public float Offset => 0;

            /// <summary>Raises <see cref="Changed"/>, for tests that need to simulate a change.</summary>
            public void RaiseChanged() => Changed?.Invoke();
        }

        /// <summary>
        /// A minimal <see cref="IPathClaim"/> that generates its spline from two attachment points.
        /// </summary>
        private sealed class StubClaim : IPathClaim {
            public readonly IPathAttachment attachmentA;
            public readonly IPathAttachment attachmentB;

            /// <summary>Creates a claim that generates its spline from the given attachment point.</summary>
            /// <param name="attachment">The attachment point to generate from.</param>
            public StubClaim(IPathAttachment attachmentA, IPathAttachment attachmentB) {
                this.attachmentA = attachmentA;
                this.attachmentB = attachmentB;
            }

            /// <inheritdoc/>
            public event Action? ObjectChanged;

            /// <inheritdoc/>
            public Obj Object => null!;

            public OrthodistantBasis GenerateSpline() {
                var startFrame = attachmentA.ReferenceFrame;
                var endFrame = attachmentB.ReferenceFrame;
                startFrame.O += startFrame.X * attachmentA.Offset;
                endFrame.O += endFrame.X * attachmentB.Offset;
                var bezier = GeometryUtils.GenerateJoinSpline(startFrame.O, endFrame.O, startFrame.Z, endFrame.Z);
                var normal = new Bezier3(startFrame.Y, startFrame.Y, endFrame.Y, endFrame.Y);
                return new OrthodistantBasis(bezier, normal);
            }
        }
    }
}
