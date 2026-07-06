using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;

namespace TranSimCS.SceneGraph {
    public sealed class SceneProxy : IMeshSource {
        private readonly SceneLeaf owner;

        public SceneProxy(SceneLeaf owner) {
            this.owner = owner;
            owner.MeshSource.OnMeshInvalidated += () =>
                OnMeshInvalidated?.Invoke();

            owner.MeshSource.OnMeshGenerated += mesh =>
                OnMeshGenerated?.Invoke(mesh);
        }

        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MultiMesh GetMesh()
            => owner.MeshSource.GetMesh();

        public void Invalidate()
            => owner.MeshSource.Invalidate();

        public BoundingBox GetBounds()
            => owner.MeshSource.GetBounds();

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag)
            => owner.MeshSource.ComputeIntersection(ray, out distance, out tag);
    }
}
