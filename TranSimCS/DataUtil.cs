using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    public static class DataUtil {
        public static readonly Random rnd = new Random();

        public static T AggregateOrDefault<T>(this IEnumerable<T> data, T def, Func<T, T, T> fn) {
            if (!data.Any()) return def;
            return data.Aggregate(fn);
        }

        public static T GetRandomElement<T>(this IList<T> list) {
            var count = list.Count;
            return list[rnd.Next(count)];
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
            long source = Convert.ToInt64(value);
            long left = Convert.ToInt64(leftFlag);
            long right = Convert.ToInt64(rightFlag);

            if (((source & left) != 0) != ((source & right) != 0))
                source ^= left | right;

            return (T)Enum.ToObject(typeof(T), source);
        }
        public static bool HasFlags<T>(this T subject, T flags) where T : struct, Enum {
            long a = Convert.ToInt64(subject);
            long b = Convert.ToInt64(flags);
            return (a & b) != 0;
        }
        public static T WithFlags<T>(this T subject, T flags, bool newValue) where T : struct, Enum{
            long subject2 = Convert.ToInt64(subject);
            long flags2 = Convert.ToInt64(flags);
            if(newValue) subject2 |= flags2; else subject2 &= ~flags2;
            return (T)Enum.ToObject(typeof(T), subject2);
        }

        public static T? OrDefault<T>(T? value) {
            return value ?? default;
        }
    }
}
