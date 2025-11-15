using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Collections;

namespace TranSimCS.Model {
    public struct Quad<T>(T a, T b, T c, T d, object? tag = null): IEnumerable<T> {
        public T A = a;
        public T B = b;
        public T C = c;
        public T D = d;
        public object? Tag = tag;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<T> GetEnumerator() => Enumerator<T>.Create(A, B, C, D);

        public T[] ToArray() => [A, B, C, D];
    }
    public static class Quads {
        public static Quad<VertexPositionColorTexture> Create(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, object? tag = null)
            => new Quad<VertexPositionColorTexture>(
                new(a, color, new(0, 0)),
                new(b, color, new(1, 0)),
                new(c, color, new(1, 1)),
                new(d, color, new(0, 1)),
            tag );
    }
}
