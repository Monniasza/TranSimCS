using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        /// <summary>
        /// Root scene graph. Contains at least <see cref="NodesGraph"/>, <see cref="SectionsGraph"/> and <see cref="SegmentsGraph"/>
        /// </summary>
        public readonly SceneTree RootGraph;
        public readonly SceneTree NodesGraph;
        public readonly SceneTree SectionsGraph;
        public readonly SceneTree SegmentsGraph;
    }
}
