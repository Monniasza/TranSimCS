using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Menus.InGame {
    public static class SelectionUtils {
        public static void AddAddLaneSelectors(InGameMenu game) {
            var renderBin = new MeshBuilder<SimpleMaterial, VertexPositionColorTexture>();
            renderBin.Name = "addLanes";
            renderBin.Material = new SimpleMaterial(Assets.Road);
            AddAddLaneSelectors(renderBin, game);
            game.renderHelper.AddElement(renderBin.Create());
        }
        public static void AddAddLaneSelectors(MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, InGameMenu game) => AddAddLaneSelectors(mesh, game.World.Nodes.data, game.configuration.LaneSpec.Width);
        public static void AddAddLaneSelectors(MeshBuilder<SimpleMaterial, VertexPositionColorTexture> mesh, IEnumerable<RoadNode> nodes, float width) {
            foreach (RoadNode node in nodes) 
                RoadRenderer.CreateAddLanes(node, mesh, width);
            
        }
    }
}
