using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNet.Collections.Generic;

namespace TranSimCS.Collections {
    public static class CollectionMethods {
        public static void AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> rows) {
            foreach (var row in rows) dict.Add(row.Key, row.Value);
        }
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> data) {
            foreach(var element in data) collection.Add(element);
        }
        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> data) {
            foreach(var element in data) collection.Remove(element);
        }
        public static void TransformInPlace<T>(this IList<T> list, Func<T, T> transform) {
            for (int i = 0; i < list.Count; i++) {
                list[i] = transform(list[i]);
            }
        }
        public static void FilterInPlace<T>(this HashSet<T> set, Predicate<T> data) {
            set.RemoveWhere(x => !data(x));
        }

        public static Dictionary<TKey, List<TValue>> QuickGroup<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keyMapper) {
            Dictionary<TKey, List<TValue>> result = new();
            foreach (var item in source) {
                var groupKey = keyMapper(item);
                if(result.TryGetValue(groupKey, out var group)) {
                    group.Add(item);
                } else {
                    var group2 = new List<TValue>();
                    group2.Add(item);
                    result[groupKey] = group2;
                }
            }
            return result;
        }
    }
}
