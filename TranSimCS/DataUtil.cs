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
    }
}
