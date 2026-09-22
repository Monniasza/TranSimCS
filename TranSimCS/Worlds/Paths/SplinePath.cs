using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using TranSimCS.Cars;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spatial;

namespace TranSimCS.Worlds.Paths {

    /// <summary>
    /// A topology-independent representation of a path that can be taken by various vehicles.
    /// Can't be deleted manually, must be <see cref="Displace(SplinePath?)"/>d for it to be later collected.
    /// 
    /// When a path is created, it is initially <see cref="PathState.Unused"/>.
    /// This means the path is not claimed by any object and will be collected if unoccupied
    /// 
    /// For path to be used, it needs to be claimed by a <see cref="IPathClaim"/>.
    /// After claiming, it becomes <see cref="PathState.Active"/> until it is <see cref="Displace(SplinePath?)"/>d
    /// 
    /// When the path becomes <see cref="PathState.Active"/>, it listens to the claimant's <see cref="IPathClaim.ObjectChanged"/>.
    /// When this <see cref="SplinePath"/> receives a <see cref="IPathClaim.ObjectChanged"/>,
    /// the path does not delete its splines. Instead, it marks the splines dirty, and regenerated them if requested.
    /// 
    /// Whenever any object requests the associated spline, the <see cref="SplinePath"/> checks if it is dirty.
    /// If it is dirty, it regenerates the spline with <see cref="IPathClaim.GenerateSpline()"/>, assigns the generated spline and removes the dirty flag.
    /// 
    /// When the path is <see cref="PathState.Active"/>, it will be found by cars when planning routes.
    /// This means the road is open for traffic unless a detour is in place.
    /// 
    /// When it is the time to remove the path, it is immediately <see cref="Delete()"/>d or <see cref="Demolish()"/>ed.
    /// Additionally, the <see cref="PathSystem"/> does not have the right methods.
    /// Instead, the path is <see cref="Displace(SplinePath?)"/>d. The argument is a replacement for cars. <see langword="null"/> is used if no suitable replacement is avaialable.
    /// If the replacement is <see langword="null"/>, the cars will use spatial search to find nearset option and continue from there.
    /// 
    /// After the path is <see cref="Displace(SplinePath?)"/>d, it deletes the claimant and becomes <see cref="PathState.Orphaned"/>.
    /// This means that the path is no longer available for new traffic, but any traffic still on the path has time to leave it.
    /// Such paths still can be referenced by objects, like cars, but they won't be able to be found by the spatial index, only by GUID or direct reference.
    /// 
    /// When a path is <see cref="PathState.Orphaned"/> or <see cref="PathState.Unused"/> and additionally it has no cars on it, then it becomes eligible for collection.
    /// 
    /// After a path becomes eligible for collection, it is deleted from the GUID cross-reference, and marked <see cref="PathState.Deleted"/>
    /// The <see cref="PathState.Deleted"/> is a safeguard against using such paths for simulation or serialization. Attempting to load in such paths will throw <see cref="KeyNotFoundException"/>.
    /// 
    /// </summary>
    public class SplinePath: Obj, IObjMesh {
        //Generated data
        /// <summary>
        /// The starting attachment of this claim
        /// </summary>
        public HalfLane? Start { get; private set; }
        /// <summary>
        /// The ending attachment of this claim
        /// </summary>
        public HalfLane? End { get; private set; }

        /// <summary>
        /// The claim that generates this path's spline, or <see langword="null"/> when the path is not
        /// claimed. A path is claimed while it is <see cref="PathState.Active"/>.
        /// </summary>
        public IPathClaim? Claimant { get; private set; }

        /// <summary>
        /// Whether the cached spline is out of date and has to be regenerated.
        /// <para>
        /// Set when the path is claimed, when the claim reports a change, and when the path is marked
        /// dirty explicitly. Cleared by <see cref="GetSpline"/> once the spline has been regenerated.
        /// </para>
        /// </summary>
        public bool Dirty { get; private set; }

        /// <summary>
        /// The cached spline lookup table of this path.
        /// <para>
        /// Only valid once <see cref="GetSpline"/> has been called at least once. Read the spline
        /// through <see cref="GetSpline"/> rather than through this property, so that a stale table is
        /// never used.
        /// </para>
        /// </summary>
        public OrthodistantLUT? LUT { get; internal set; }

        /// <summary>
        /// The state of this path in its lifecycle.
        /// </summary>
        public PathState CurrentState { get; private set; }

        /// <summary>
        /// The path that replaced this one when it was displaced, or <see langword="null"/> when this
        /// path was orphaned without a replacement.
        /// <para>
        /// Traffic that is on an orphaned path can follow this reference to continue onto the path that
        /// took its place.
        /// </para>
        /// </summary>
        public SplinePath? ReplacementPath { get; private set; }

        /// <summary>
        /// The lane specification of this path, describing the traffic that may use it.
        /// </summary>
        public LaneSpec Spec { get; set; }

        /// <summary>
        /// Whether every attachment point of this path is still alive.
        /// <para>
        /// A path whose attachment points are not all alive can no longer be regenerated, so it must not
        /// be used for new traffic. Traffic already on the path may still finish leaving it.
        /// </para>
        /// </summary>
        public bool AreAttachmentsAlive => Claimant == null || Claimant.IsAlive();

        internal HashSet<Car> _cars = new();
        public ReadOnlySet<Car> Cars { get; private set; }

        /// <summary>
        /// Claims this path for the given claimant, making it <see cref="PathState.Active"/>.
        /// <para>
        /// A claimed path listens to its claimant, so that a change to the thing the path is anchored to
        /// marks the path dirty. An active path is added to the world's spatial index, which is what
        /// makes it discoverable by traffic.
        /// </para>
        /// </summary>
        /// <param name="claimant">The claim that will generate this path's spline.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="claimant"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this path is not <see cref="PathState.Unused"/>, so it has already been claimed
        /// or has been orphaned or deleted.
        /// </exception>
        public void Claim(IPathClaim claimant) {
            ArgumentNullException.ThrowIfNull(claimant, nameof(claimant));
            switch (CurrentState) {
                case PathState.Unused:
                    Claimant = claimant;
                    CurrentState = PathState.Active;
                    Dirty = true;
                    Start = claimant.Start;
                    End = claimant.End;
                    //Listen to the claim, so that a change to the thing the path is anchored to marks the
                    //path dirty and its spline is regenerated on next access.
                    Claimant.ObjectChanged += OnClaimantChanged;
                    World?.Paths._pathsSpatial.Add(this, true);
                    break;
                case PathState.Active:
                    throw new InvalidOperationException("Claiming an already claimed Path");
                case PathState.Orphaned:
                case PathState.Deleted:
                    throw new InvalidOperationException("Re-claiming a displaced path. If the path has a replacement, use it instead.");
                default:
                    Debug.Fail($"Invalid path state: {CurrentState}");
                    return;
            }
        }
        /// <summary>
        /// Orphans this path, optionally recording the path that replaced it.
        /// <para>
        /// An orphaned path keeps its last generated spline and stays resolvable by GUID and by direct
        /// reference, so that traffic already on it can leave, but it leaves the spatial index so that
        /// no new traffic is routed onto it.
        /// </para>
        /// </summary>
        /// <param name="replacement">
        /// The path that took this one's place, or <see langword="null"/> when there is none.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this path is <see cref="PathState.Unused"/>, so it was never claimed, or when it
        /// has already been orphaned or deleted.
        /// </exception>
        public void Displace(SplinePath? replacement) {
            switch (CurrentState) {
                case PathState.Unused:
                    throw new InvalidOperationException("Displacing an unclaimed Path");
                case PathState.Active:
                    ReplacementPath = replacement;
                    //Stop listening to the claim: an orphaned path no longer follows the road network.
                    Claimant.ObjectChanged -= OnClaimantChanged;
                    Claimant = null;
                    CurrentState = PathState.Orphaned;
                    Dirty = false;
                    World?.Paths._pathsSpatial.Remove(this);
                    break;
                case PathState.Orphaned:
                case PathState.Deleted:
                    throw new InvalidOperationException("Displacing a deleted Path");
                default:
                    Debug.Fail($"Invalid path state: {CurrentState}");
                    return;
            }
        }

        /// <summary>
        /// Marks this path's spline as out of date, so that it is regenerated on next access.
        /// <para>
        /// Called when something the path depends on changes, such as the lane specification of the
        /// strip that owns it. Marking a path dirty does not regenerate anything immediately; the
        /// regeneration happens lazily in <see cref="GetSpline"/>.
        /// </para>
        /// </summary>
        public void MarkDirty() {
            if (CurrentState == PathState.Deleted) return;
            Dirty = true;
        }

        /// <summary>
        /// Marks this path dirty when the claim reports that the thing it is anchored to has changed.
        /// <para>
        /// This is what makes a path follow the road network: moving a road node, or changing a lane,
        /// raises the claim's change event, which lands here and invalidates the cached spline.
        /// </para>
        /// </summary>
        private void OnClaimantChanged() => MarkDirty();

        /// <summary>
        /// Orphans this path if any of its attachment points has died.
        /// <para>
        /// This is the recovery path for a segment being deleted from under a car without the car being
        /// notified. The path is not deleted: it becomes <see cref="PathState.Orphaned"/>, which keeps it
        /// resolvable by GUID and by direct reference so that traffic already on it can leave, while
        /// removing it from the spatial index so that no new traffic is routed onto it.
        /// </para>
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the path was orphaned by this call, otherwise <see langword="false"/>.
        /// </returns>
        public bool OrphanIfAttachmentsDead() {
            if (CurrentState != PathState.Active) return false;
            if (AreAttachmentsAlive) return false;
            Displace(null);
            return true;
        }

        /// <summary>
        /// Gets the spline of this path, regenerating it if it is dirty.
        /// <para>
        /// This never throws for a path whose attachment points have died: a dead attachment point means
        /// the path is orphaned, and an orphaned path keeps serving its last generated spline so that
        /// traffic already on it can finish leaving. Only a <see cref="PathState.Deleted"/> path throws.
        /// </para>
        /// </summary>
        /// <returns>The spline lookup table for this path.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this path has been collected and is <see cref="PathState.Deleted"/>.
        /// </exception>
        public OrthodistantLUT GetSpline() {
            if (CurrentState == PathState.Deleted)
                throw new InvalidOperationException("Using a Deleted path");
            if (Dirty) {
                if (Claimant == null) {
                    //The claimant went away without the spline being regenerated. Keep the last good
                    //spline rather than throwing, so that traffic already on the path can leave it.
                    Dirty = false;
                    return LUT;
                }
                var spline = Claimant.GenerateSpline();
                LUT = new(spline);
                Dirty = false;
            }
            return LUT;
        }

        /// <summary>
        /// Marks the path as deleted, jsut before removal from the tree. Used by the <see cref="PathSystem"/> garbage collector
        /// </summary>
        internal void Collect() {
            Debug.Assert(Claimant == null);
            Debug.Assert(Cars.Count == 0);
            Debug.Assert(CurrentState is PathState.Unused or PathState.Orphaned);
            CurrentState = PathState.Deleted;
            Start = null;
            End = null;
            if (Claimant is LaneStripPathClaim laneStripClaim)
                laneStripClaim.Detach();
        }
        

        /// <summary>
        /// Creates a path anchored to the given attachment points.
        /// </summary>
        /// <param name="claimant">
        /// The claim that generates this path's spline, or <see langword="null"/> for an unclaimed path.
        /// </param>
        /// <param name="guid">
        /// The GUID to assign to this path, or <see langword="null"/> to generate a new one.
        /// Passing the GUID of a previously saved path is what allows the same path to be reused after
        /// loading a world.
        /// </param>
        public SplinePath(IPathClaim? claimant, Guid? guid, HalfLane? startAttachment, HalfLane? endAttachment) {
            if (guid != null) Guid = guid.Value;
            CurrentState = PathState.Unused;
            Cars = new(_cars);
            Start = startAttachment;
            End = endAttachment;
            if (claimant != null) Claim(claimant);
            
        }

        //SPATIAL

        /// <summary>
        /// Raised when the geometry of this path changes, so that the spatial index can be updated.
        /// </summary>
        public event MeshInvalidationCallback GeometryChanged;

        /// <summary>
        /// Generates the renderable geometry of this path.
        /// <para>
        /// Paths are not drawn, so this does nothing. It exists because the spatial index requires an
        /// <see cref="IObjMesh"/> implementation.
        /// </para>
        /// </summary>
        /// <param name="target">The render target, unused.</param>
        public void GenerateGeometry(RenderTarget target) {
            //unused
        }

        /// <summary>
        /// Computes the world-space bounding box of this path.
        /// <para>
        /// The box is derived from the path's spline, so it covers the whole centre line. It is used by
        /// the spatial index to answer "which paths are near here" queries.
        /// </para>
        /// <para>
        /// A path that has no spline yet, or whose spline cannot be generated, returns an empty box
        /// rather than throwing. An empty box is ignored by the spatial index, which is the correct
        /// behaviour for a path that has no geometry to index.
        /// </para>
        /// </summary>
        /// <returns>The bounding box of this path, or an empty box when it has no geometry.</returns>
        public AABB GetBounds() {
            if (CurrentState == PathState.Deleted) return default;
            if (Claimant == null) return default;

            OrthodistantLUT lut;
            try {
                lut = GetSpline();
            } catch (InvalidOperationException) {
                //The path has no usable geometry, for example because its attachment points died before
                //a spline was ever generated. An empty box keeps it out of the spatial index.
                return default;
            }

            var box = default(AABB);
            bool first = true;
            foreach (var key in lut.Forward) {
                var point = key.Y.ToXYZ();
                var pointBox = new AABB(point, point);
                box = first ? pointBox : AABB.CreateMerged(box, pointBox);
                first = false;
            }
            return box;
        }

        /// <summary>
        /// Tests whether a ray hits this path.
        /// <para>
        /// Paths are not pickable, so this always rejects.
        /// </para>
        /// </summary>
        /// <param name="ray">The ray to test.</param>
        /// <param name="distance">Always set to zero.</param>
        /// <param name="tag">Always set to <see langword="null"/>.</param>
        /// <returns>Always <see langword="false"/>.</returns>
        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) => IBVHElement.Reject(ray, out distance, out tag);

        //Car cache. Maintained by CarStack
        internal List<CarEntry> _carsOnStrip = [];
        public IReadOnlyList<CarEntry> CarsOnStrip => _carsOnStrip.AsReadOnly();
        internal void InsertCar(Car car) {

        }
        internal void RemoveCar(Car car) {
            for (int i = 0; i < _carsOnStrip.Count; i++) {
                var entry = _carsOnStrip[i];
                if (entry.car == car) {
                    _carsOnStrip.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Find the index of the first car ahead of <paramref name="position"/>, or <see cref="CarsOnStrip"/>.Count, if not found
        /// </summary>
        public int FindFirstAheadIndex(float position) {
            int min = 0;
            int max = _carsOnStrip.Count;

            while (min < max) {
                int mid = (min + max) >> 1;
                if (_carsOnStrip[mid].positionOnStrip <= position)
                    min = mid + 1;
                else
                    max = mid;
            }
            return min;
        }
        /// <summary>
        /// Find the index of the last car behind <paramref name="position"/>, or -1 if not found
        /// </summary>
        public int FindLastBehindIndex(float position) {
            int min = 0;
            int max = _carsOnStrip.Count;

            while (min < max) {
                int mid = (min + max) >> 1;

                if (_carsOnStrip[mid].positionOnStrip < position)
                    min = mid + 1;
                else
                    max = mid;
            }

            return min - 1;
        }
    }

    /// <summary>
    /// The lifecycle state of a <see cref="SplinePath"/>.
    /// <para>
    /// A path moves from <see cref="Unused"/> to <see cref="Active"/> when it is claimed, from
    /// <see cref="Active"/> to <see cref="Orphaned"/> when the thing it is anchored to goes away, and
    /// from <see cref="Orphaned"/> to <see cref="Deleted"/> when the garbage collector reclaims it.
    /// </para>
    /// </summary>
    public enum PathState {
        /// <summary>
        /// The path has not been claimed yet.
        /// Eligible for collection if no vehicles occupy it for at least 1 second.
        /// Claim a <see cref="SplinePath"/> in this state to make it <see cref="PathState.Active"/>
        /// </summary>
        Unused,
        /// <summary>
        /// A path with an assigned claimant
        /// </summary>
        Active,
        /// <summary>
        /// The path has been displaced.
        /// Eligible for collection if no vehicles occupy it.
        /// After collection, it becomes <see cref="Deleted"/>
        /// </summary>
        Orphaned,
        /// <summary>
        /// The path has been deleted from the GUID table and is no longer valid
        /// </summary>
        Deleted
    }
}
