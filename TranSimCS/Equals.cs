using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS {
    public static class Equals {
        /// <summary>
        /// Equality function similar to JS === operator.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>are objects equal by both value and type?</returns>
        public static bool TripleEquals(object a, object b) {
            return Equals(a, b) && a.GetType() == b.GetType();
        }
    }
}
