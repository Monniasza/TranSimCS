using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjLoader.Loader.Data.Elements;

namespace TranSimCS.Model {
    internal class FaceIterable(Face face) : IList<FaceVertex> {
        public FaceVertex this[int index] { get => face[index]; set => throw new NotImplementedException(); }

        public int Count => face.Count;

        public bool IsReadOnly => false;

        public void Add(FaceVertex item) {
            face.AddVertex(item);
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(FaceVertex item) {
            foreach (var e in this) {
                if(FaceVertsEqual(e, item)) return true;
            }
            return false;
        }

        public static bool FaceVertsEqual(FaceVertex a, FaceVertex b) {
            return a.VertexIndex == b.VertexIndex && a.TextureIndex == b.TextureIndex && a.NormalIndex == b.NormalIndex;
        }

        public void CopyTo(FaceVertex[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public IEnumerator<FaceVertex> GetEnumerator() {
            throw new NotImplementedException();
        }

        public int IndexOf(FaceVertex item) {
            throw new NotImplementedException();
        }

        public void Insert(int index, FaceVertex item) {
            throw new NotImplementedException();
        }

        public bool Remove(FaceVertex item) {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index) {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
