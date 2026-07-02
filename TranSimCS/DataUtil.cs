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
        public static int SwapFlags(int source, int leftFlag, int rightFlag) {
            bool left = (source & leftFlag) != 0;
            bool right = (source & rightFlag) != 0;

            if (left != right)
                source ^= leftFlag | rightFlag;

            return source;
        }
        public static T SwapFlags<T>(T value, T leftFlag, T rightFlag)
    where T : struct, Enum {
            ulong source = Convert.ToUInt64(value);
            ulong left = Convert.ToUInt64(leftFlag);
            ulong right = Convert.ToUInt64(rightFlag);

            if (((source & left) != 0) != ((source & right) != 0))
                source ^= left | right;

            return (T)Enum.ToObject(typeof(T), source);
        }

        public static T? OrDefault<T>(T? value) {
            return value ?? default;
        }
    }
}
