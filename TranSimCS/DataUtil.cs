using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    public static class DataUtil {
        public static T AggregateOrDefault<T>(this IEnumerable<T> data, T def, Func<T, T, T> fn) {
            if (!data.Any()) return def;
            return data.Aggregate(fn);
        }

        public static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

        public static void Swap<T>(T[] array, int a, int b) {
            var tmp = array[a];
            array[a] = array[b];
            array[b] = tmp;
        }

        public static T? OrDefault<T>(T? value) {
            return value ?? default;
        }
    }
}
