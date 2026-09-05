using System.Collections.Generic;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet;

namespace TranSimCS.Select {
    public static class SelectionUtils {
        public static void AddAddLaneSelectors(MultiMesh meshes, SilkNetTest game) {
            var target = meshes.GetOrCreateRenderBinForced(Materials.Add);
            AddAddLaneSelectors(target, game.World.Nodes.data, game.LaneSpec.Width);
        }
        public static void AddAddLaneSelectors(Mesh mesh, IEnumerable<RoadNode> nodes, float width) {
            foreach (RoadNode node in nodes) 
                NodeRenderer.CreateAddLanes(node, mesh, width);
            
        }
    }
}
