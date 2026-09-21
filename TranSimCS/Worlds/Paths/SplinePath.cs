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

        public IPathClaim? Claimant { get; private set; }
        public bool Dirty { get; private set; }
        public OrthodistantLUT LUT { get; private set; }
        public PathState CurrentState { get; private set; }
        public SplinePath? ReplacementPath { get; private set; }
        public LaneSpec Spec { get; set; }

        internal HashSet<Car> _cars = new();
        public ReadOnlySet<Car> Cars { get; private set; }

        public void Claim(IPathClaim claimant) {
            ArgumentNullException.ThrowIfNull(claimant, nameof(claimant));
            switch (CurrentState) {
                case PathState.Unused:
                    Claimant = claimant;
                    CurrentState = PathState.Active;
                    Dirty = true;
                    World?.Paths._pathsSpatial.Add(this);
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
        public void Displace(SplinePath? replacement) {
            switch (CurrentState) {
                case PathState.Unused:
                    throw new InvalidOperationException("Displacing an unclaimed Path");
                case PathState.Active:
                    ReplacementPath = replacement;
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

        public OrthodistantLUT GetSpline() {
            if (CurrentState == PathState.Deleted)
                throw new InvalidOperationException("Using a Deleted path");
            if (Dirty) {
                Debug.Assert(Claimant != null, "Dirty path without a valid claimant");
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
        }
        

        public SplinePath(IPathClaim? claimant, Guid? guid) {
            CurrentState = PathState.Unused;
            Cars = new(_cars);
            if (claimant != null) Claim(claimant);
            if(guid != null) Guid = guid.Value;
        }

        //SPATIAL

        public event MeshInvalidationCallback GeometryChanged;
        public void GenerateGeometry(RenderTarget target) {
            //unused
        }
        public AABB GetBounds() {
            throw new NotImplementedException();
        }
        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) => IBVHElement.Reject(ray, out distance, out tag);
    }

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
