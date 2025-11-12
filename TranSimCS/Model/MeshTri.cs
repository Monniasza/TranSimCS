using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Model {
    /// <summary>
    /// A mesh triangle. Also includes a tag for selection purposes.
    /// </summary>
    public struct MeshTri {
        /// <summary>
        /// First vertex ID
        /// </summary>
        public int A;
        /// <summary>
        /// Second vertex ID
        /// </summary>
        public int B;
        /// <summary>
        /// Third vertex ID
        /// </summary>
        public int C;
        /// <summary>
        /// Selection tag. null for no selection.
        /// </summary>
        public object? Tag;
    }
}
