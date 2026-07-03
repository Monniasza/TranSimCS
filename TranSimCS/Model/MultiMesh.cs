using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.ModelOld;

namespace TranSimCS.Model {
    /// <summary>
    /// A collections of meshes, each with its own texture
    /// </summary>
    public class MultiMesh {
        //List of data to render
        private Dictionary<SimpleMaterial, Mesh> _renderBins = [];
        public IDictionary<SimpleMaterial, Mesh> RenderBins => new ReadOnlyDictionary<SimpleMaterial, Mesh>(_renderBins);

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
        }
        public void ClearAll() {
            _renderBins.Clear();
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
        }
    }
}
