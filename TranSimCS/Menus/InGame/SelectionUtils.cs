using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Tools;

namespace TranSimCS.Menus.InGame {
    public static class SelectionUtils {
        public static void AddAddLaneSelectors(InGameMenu game) => AddAddLaneSelectors(game.SelectorObjects.GetOrCreateRenderBinForced(Assets.Add), game);
        public static void AddAddLaneSelectors(Mesh mesh, InGameMenu game) => AddAddLaneSelectors(mesh, game.World.Nodes.data, StripTool.GetActualLaneSpec(game).Width);
        public static void AddAddLaneSelectors(Mesh mesh, IEnumerable<RoadNode> nodes, float width) {
            foreach (RoadNode node in nodes) 
                NodeRenderer.CreateAddLanes(node, mesh, width);
            
        }
    }
}
