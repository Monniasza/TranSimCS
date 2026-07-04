using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Collections;
using TranSimCS.ModelOld;
using TranSimCS.Spatial;

namespace TranSimCS.Model {
    /// <summary>
    /// A collections of meshes, each with its own texture
    /// </summary>
    public class MultiMesh: IBVHElement {
        //List of data to render
        private Dictionary<SimpleMaterial, Mesh> _renderBins = [];
        public IDictionary<SimpleMaterial, Mesh> RenderBins => new ReadOnlyDictionary<SimpleMaterial, Mesh>(_renderBins);
        public readonly ObservableCollection<MeshInstance> meshInstances = new ObservableCollection<MeshInstance>();

        public MultiMesh() {
            meshInstances.CollectionChanged += (s, e) => InvalidateAccelerationStructure();
        }

        //SPATIAL INDEXING
        private ElementBVH<IBVHElement>? bvh;
        internal ElementBVH<IBVHElement> GetAccelerationStructure() {
            if(bvh != null) return bvh;
            List<IBVHElement> list = [];
            list.AddRange(RenderBins.Values.Cast<IBVHElement>());
            list.AddRange(meshInstances.Cast<IBVHElement>());
            bvh = new ElementBVH<IBVHElement>(list);
            return bvh;
        }
        internal void InvalidateAccelerationStructure() => bvh = null;        
        public BoundingBox GetBounds() => GetAccelerationStructure().Bounds;

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => GetAccelerationStructure().RayIntersect(ray, out distance, out tag);

        //The helper method to add a render bin for a specific texture and populate it with vertices and indices.
        public Mesh GetOrCreateRenderBinForced(SimpleMaterial texture) {
            ArgumentNullException.ThrowIfNull(texture.Texture, nameof(texture.Texture));
            return GetOrCreateRenderBin(texture, null);
        }
        public Mesh? GetOrCreateRenderBin(SimpleMaterial? texture) {
            if (texture?.Texture == null) return null;
            return GetOrCreateRenderBin(texture.Value, null);
        }

        public bool TryGetOrCreateRenderBin(SimpleMaterial? texture, out Mesh mesh) {
            mesh = GetOrCreateRenderBin(texture.Value);
            return mesh != null;
        }

        public void Clear() {
            foreach (var renderBin in _renderBins.Values)
                renderBin.Clear();
            meshInstances.Clear();
        }
        public void ClearAll() {
            _renderBins.Clear();
            meshInstances.Clear();
        }
        public Mesh GetOrCreateRenderBin(SimpleMaterial texture, Action<Mesh>? action) {
            if (!_renderBins.TryGetValue(texture, out var renderBin)) {
                renderBin = new Mesh();
                _renderBins[texture] = renderBin;
            }
            action?.Invoke(renderBin);
            return renderBin;
        }
        public void AddAll(MultiMesh meshes) {
            foreach (var kv in meshes.RenderBins) {
                Mesh renderBin = GetOrCreateRenderBinForced(kv.Key);
                renderBin.DrawModel(kv.Value);
            }
            meshInstances.AddRange(meshes.meshInstances);
        }
    }
}
