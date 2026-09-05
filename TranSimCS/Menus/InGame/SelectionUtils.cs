using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet;
using TranSimCS.Tools;

namespace TranSimCS.Menus.InGame {
    public static class SelectionUtils {
        public static void AddAddLaneSelectors(MultiMesh meshes, SilkNetTest game) {
            var target = meshes.GetOrCreateRenderBinForced(Materials.Add);
            AddAddLaneSelectors(target, game.World.Nodes.data, game.LaneSpec.Width);
        }
        public static void AddAddLaneSelectors(InGameMenu game) => AddAddLaneSelectors(game.SelectorObjects.GetOrCreateRenderBinForced(Materials.Add), game);
        public static void AddAddLaneSelectors(Mesh mesh, InGameMenu game) => AddAddLaneSelectors(mesh, game.World.Nodes.data, game.configuration.LaneSpec.Width);
        public static void AddAddLaneSelectors(Mesh mesh, IEnumerable<RoadNode> nodes, float width) {
            foreach (RoadNode node in nodes) 
                NodeRenderer.CreateAddLanes(node, mesh, width);
            
        }
    }
}
