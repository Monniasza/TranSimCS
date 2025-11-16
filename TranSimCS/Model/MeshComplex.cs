using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Collections;

namespace TranSimCS.Model {
    public class MeshComplex {
        public readonly HashSet<MeshElement> Elements;

        public MeshComplex() {
            Elements = [];
        }

        public void AddElement(MeshElement meshElement) {
            Elements.Add(meshElement);
        }
        public bool Remove(MeshElement element) => Elements.Remove(element);
        public void Clear() => Elements.Clear();
        public void AddAll(MeshComplex other) => Elements.AddRange(other.Elements);
    }
}
