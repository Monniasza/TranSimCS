using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Model;
using TranSimCS.Spatial;

namespace TranSimCS.SceneGraph {
    public interface IMeshSource: IBVHElement {
        public event Action<MultiMesh>? OnMeshGenerated;
        public event Action? OnMeshInvalidated;

        public MultiMesh GetMesh();
        public void Invalidate();

        bool IBVHElement.ComputeIntersection(Ray ray, out float distance, out object? tag)
            => GetMesh().ComputeIntersection(ray, out distance, out tag);
        BoundingBox IBVHElement.GetBounds()
            => GetMesh().GetBounds();
    }
}
