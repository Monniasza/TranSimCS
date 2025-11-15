using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Collections;
using TranSimCS.Worlds.Car;

namespace TranSimCS.Model {
    public class MeshComplex {
        public readonly HashSet<MeshElement> Elements;

        public MeshComplex() {
            Elements = [];
        }

        public void AddElement(MeshElement meshElement) {
            Elements.Add(meshElement);
        }
        public MeshElement<TMaterial, TVertex> AddElement<TMaterial, TVertex>(MeshBuilder<TMaterial, TVertex> meshBuilder) {
            var element = meshBuilder.Create();
            Elements.Add(element);
            return element;
        }
        public bool Remove(MeshElement element) => Elements.Remove(element);
        public void Clear() => Elements.Clear();
        public void AddAll(MeshComplex other) => Elements.AddRange(other.Elements);

        public void AddTagsToAll(object? tag) {
            foreach (var element in Elements) {
                int count = element.Triangles.Length;
                for (int i = 0; i < count; i++) {
                    var triangle = element.Triangles[i];
                    triangle.Tag = tag;
                    element.Triangles[i] = triangle;
                }
            }
        }
    }
}
