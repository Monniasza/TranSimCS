using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Collections.Generic;

namespace TranSimCS.Collections {
    public static class CollectionMethods {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> data) {
            foreach(var element in data) collection.Add(element);
        }
        public static void TransformInPlace<T>(this IList<T> list, Func<T, T> transform) {
            for (int i = 0; i < list.Count; i++) {
                list[i] = transform(list[i]);
            }
        }
    }
}
