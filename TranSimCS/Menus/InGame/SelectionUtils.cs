using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Roads;

namespace TranSimCS.Menus.InGame {
    public static class SelectionUtils {
        public static void AddAddLaneSelectors(InGameMenu game) => AddAddLaneSelectors(game.SelectorObjects.GetOrCreateRenderBin(Assets.Add), game);
        public static void AddAddLaneSelectors(IRenderBin mesh, InGameMenu game) => AddAddLaneSelectors(mesh, game.World.RoadNodes, game.configuration.LaneSpec.Width);
        public static void AddAddLaneSelectors(IRenderBin mesh, IEnumerable<RoadNode> nodes, float width) {
            foreach (RoadNode node in nodes) 
                RoadRenderer.CreateAddLanes(node, mesh, width);
            
        }
    }
}
