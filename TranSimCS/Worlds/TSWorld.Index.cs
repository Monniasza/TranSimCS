using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        /// <summary>
        /// Root scene graph. Contains all selectable objects in this world.
        /// </summary>
        public readonly SceneTree RootGraph;
        /// <summary>
        /// Scene graph for road sections
        /// </summary>
        public readonly SceneTree SectionsGraph;
        /// <summary>
        /// Scene graph for Add Lane Selection components
        /// </summary>
        public readonly SceneLeaf TempSelectors;
        /// <summary>
        /// Mesh property for temporary selectors
        /// </summary>
        public Property<MultiMesh> TempSelectorsMesh;
    }
}
