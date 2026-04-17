using System;
using System.Collections;
using TranSimCS.Roads.Node;

namespace TranSimCS {
    public class Verify {
        internal static void ThrowIfNullOrContainsNull(IEnumerable data, string name) {
            ArgumentNullException.ThrowIfNull(data, name);
            foreach(var item in data)
                if (item is null) throw new ArgumentNullException(name);
        }
    }
}