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
        public MeshInstance(MultiMesh mesh, TransformQ transform) {
            Mesh = mesh;
            PositionRotation = transform;
        }

        public MeshInstance Transform(TransformQ transform) => new MeshInstance(Mesh, transform * PositionRotation);

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) {
            var inverse = PositionRotation.Inverse();
            var inverseRay = inverse.Transform(ray);
            return Mesh.ComputeIntersection(inverseRay, out distance, out tag);
        }
        public BoundingBox GetBounds() => OBB.TransformBoundingBox(Mesh.GetBounds(), PositionRotation);

        public override bool Equals(object? obj) {
            return obj is MeshInstance instance && Equals(instance);
        }

        public bool Equals(MeshInstance other) {
            return EqualityComparer<MultiMesh>.Default.Equals(Mesh, other.Mesh) &&
                   EqualityComparer<TransformQ>.Default.Equals(PositionRotation, other.PositionRotation);
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
