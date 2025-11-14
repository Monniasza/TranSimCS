using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Collections {
    public static class Enumerator<T>{
        public static IEnumerator<T> Create(params T[] data) => ((IEnumerable<T>)data).GetEnumerator();
    }
}
