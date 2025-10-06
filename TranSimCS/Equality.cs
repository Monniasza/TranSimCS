using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Tools;

namespace TranSimCS {
    public static class Equality {
        /// <summary>
        /// Equality function similar to JS === operator.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>are objects equal by both value and type?</returns>
        public static bool TripleEquals(object a, object b) {
            return Equals(a, b) && a.GetType() == b.GetType();
        }

        public static bool ArrayEqualsWithNull<T>(T[] a, T[] b) {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return Enumerable.SequenceEqual(a, b);
        }
        public static bool DeepArrayEqualsWithNull((object[], string)[] a, (object[], string)[] b) {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                var A = a[i];
                var B = b[i];
                if (A.Item2 != B.Item2) return false;
                if (!Enumerable.SequenceEqual(A.Item1, B.Item1)) return false;
            }
            return true;
        }

        public static EqualityComparer<U> By<T, U>(Func<U, T> byFunc, EqualityComparer<T>? eq = null){
            var eq2 = eq ?? EqualityComparer<T>.Default;
            return EqualityComparer<U>.Create((a, b) => eq2.Equals(byFunc(a), byFunc(b)));
        }

        internal static IEqualityComparer<T> ReferenceEqualComparer<T>() {
            return EqualityComparer<T>.Create((a, b) => Object.ReferenceEquals(a, b));
        }
    }
}
