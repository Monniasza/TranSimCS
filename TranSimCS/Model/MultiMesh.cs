using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    /// <summary>
    /// A collections of meshes, each with its own texture
    /// </summary>
    public class MultiMesh {
        //List of data to render
        private Dictionary<Texture2D, Mesh> _renderBins = [];
        public IDictionary<Texture2D, Mesh> RenderBins => new ReadOnlyDictionary<Texture2D, Mesh>(_renderBins);

        //The helper method to add a render bin for a specific texture and populate it with vertices and indices.
        public Mesh GetOrCreateRenderBin(Texture2D texture) {
            return GetOrCreateRenderBin(texture, null);
        }
        public void Clear() {
            foreach (var renderBin in _renderBins.Values)
                renderBin.Clear();
        }
        public void ClearAll() {
            _renderBins.Clear();
        }
        public Mesh GetOrCreateRenderBin(Texture2D texture, Action<Mesh> action) {
            if (!_renderBins.TryGetValue(texture, out var renderBin)) {
                renderBin = new Mesh();
                _renderBins[texture] = renderBin;
            }
            action?.Invoke(renderBin);
            return renderBin;
        }
        public void AddAll(MultiMesh meshes) {
            foreach (var kv in meshes.RenderBins) {
                IRenderBin renderBin = GetOrCreateRenderBin(kv.Key);
                renderBin.DrawModel(kv.Value);
            }
        }
    }
}
