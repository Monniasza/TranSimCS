using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        /// <summary>
        /// Root scene graph. Contains all selectable objects in this world.
        /// </summary>
        public readonly SceneTree RootGraph;
        /// <summary>
        /// Scene graph for Add Lane Selection components
        /// </summary>
        public readonly SceneLeaf TempSelectors;
        /// <summary>
        /// Mesh property for temporary selectors
        /// </summary>
        public Property<MeshComplex> TempSelectorsMesh;

        public List<LaneStrip> FindLaneStrips(LaneEnd end) {
            var nodeEnd = end.RoadNodeEnd;
            List<LaneStrip> result = [];
            foreach (var connection in nodeEnd.ConnectedSegments) {
                foreach (var strip in connection.Lanes) {
                    if(strip.IsConnected(end)) result.Add(strip);
                }
            }
            return result;
        }
    }
}
