using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Spatial;
using TranSimCS.Worlds.Cars;

namespace TranSimCS.Worlds.Paths {
    /// <summary>
    /// A topology-independent representation of a path that can be taken by various vehicles 
    /// </summary>
    public class SplinePath: Obj, IObjMesh {
        //Generated data

        public Obj? Claimant { get; private set; }
        public OrthodistantLUT LUT { get; private set; }
        public PathState CurrentState { get; private set; }
        public SplinePath? ReplacementPath { get; private set; }
        public LaneSpec Spec { get; private set; }

        internal HashSet<Car> _cars = new();


        public ReadOnlySet<Car> Cars { get; private set; }

        public void Claim(Obj claimant) {
            ArgumentNullException.ThrowIfNull(claimant, nameof(claimant));
            switch (CurrentState) {
                case PathState.Unused:
                    Claimant = claimant;
                    CurrentState = PathState.Active;
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
        /// Marks the path as deleted, jsut before removal from the tree. Used by the <see cref="PathSystem"/> garbage collector
        /// </summary>
        internal void Collect() {
            Debug.Assert(Claimant == null);
            Debug.Assert(Cars.Count == 0);
            Debug.Assert(CurrentState is PathState.Unused or PathState.Orphaned);
            CurrentState = PathState.Deleted;
        }
        

        public SplinePath(Obj? claimant, Guid? guid, OrthodistantLUT lut, LaneSpec spec) {
            ArgumentNullException.ThrowIfNull(lut, nameof(lut));
            LUT = lut;
            Spec = spec;
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
        public BoundingBox GetBounds() {
            throw new NotImplementedException();
        }
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => IBVHElement.Reject(ray, out distance, out tag);
    }

    public enum PathState {
        Unused, Active, Orphaned, Deleted
    }
}
