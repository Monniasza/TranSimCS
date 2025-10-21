using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    public static class CollectionMethods {
        public static void AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> rows) {
            foreach (var row in rows) dict.Add(row.Key, row.Value);
        }
        public static void TransformInPlace<T>(this IList<T> list, Func<T, T> transform) {
            for (int i = 0; i < list.Count; i++) {
                list[i] = transform(list[i]);
            }
        }
    }
}
