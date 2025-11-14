using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
}
