using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Spatial;

namespace TranSimCS.Model {
    public struct MeshInstance : IEquatable<MeshInstance>, IBVHElement {
        public MultiMesh Mesh;
        public TransformQ PositionRotation;
        public object? CoverTag;
        public bool OverrideChildTags;

        public MeshInstance(MultiMesh mesh, TransformQ transform, object? coverTag = null, bool overrideChildTags = false) {
            Mesh = mesh;
            PositionRotation = transform;
            CoverTag = coverTag;
            OverrideChildTags = overrideChildTags;
        }

        public MeshInstance Transform(TransformQ transform) {
            var result = this;
            result.PositionRotation = transform * PositionRotation;
            return result;
        }

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) {
            var inverse = PositionRotation.Inverse();
            var inverseRay = inverse.Transform(ray);
            var isIntersecting = Mesh.ComputeIntersection(inverseRay, out distance, out tag);
            if(!isIntersecting) return false;
            if (OverrideChildTags || tag == null) tag = CoverTag;
            return true;
        }
        public BoundingBox GetBounds() => OBB.TransformBoundingBox(Mesh.GetBounds(), PositionRotation);

        public override bool Equals(object? obj) {
            return obj is MeshInstance instance && Equals(instance);
        }

        public bool Equals(MeshInstance other) {
            return Mesh == other.Mesh && PositionRotation == other.PositionRotation &&
                CoverTag == other.CoverTag && OverrideChildTags == other.OverrideChildTags;
        }        

        public override int GetHashCode() {
            return HashCode.Combine(Mesh, PositionRotation);
        }

        public static bool operator ==(MeshInstance left, MeshInstance right) {
            return left.Equals(right);
        }

        public static bool operator !=(MeshInstance left, MeshInstance right) {
            return !(left == right);
        }
    }
}
