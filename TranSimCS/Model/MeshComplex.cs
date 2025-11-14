using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public class MeshComplex {
        public readonly Dictionary<string, MeshElement> Elements;

        public MeshComplex() {
            Elements = [];
        }

        public void AddElement(MeshElement<SimpleMaterial, VertexPositionColorTexture> meshElement) {
            Elements.Add(meshElement.Name, meshElement);
        }
        public bool RemoveKey(string key) => Elements.Remove(key);
        public void Replace(MeshElement element) => Elements[element.Name] = element;
    }
}
