using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Roads.Node;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class AddNodeTool(InGameMenu menu) : ITool {
        string ITool.Name => "Add road nodes";

        string ITool.Description => NewlyCreatedNode != null ? "Click to set direction of the newly built node"
            : Reference == null ? "Click on a node to set direction from it, click elsewhere to set direction manually"
            : "Click to place a node. The reference will not be reset after placement";

        public (object[], string)[] PromptKeys() {
            (object[], string) countPrompt = ([Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9], " to set number of lanes");
            List<(object[], string)> keys = [countPrompt];
            if (Reference == null) {
                keys.Add(([MouseButton.Left], "on a node to set direction from it"));
                keys.Add(([MouseButton.Left], "elsewhere to set direction manually"));
            } else {
                keys.Add(([MouseButton.Left], "to place a node. The reference will not be reset after placement"));
                keys.Add(([MouseButton.Right], "to cancel placement"));
            }
            return keys.ToArray();
        }

        /// <summary>
        /// Set if the new node has to be rotated after placement
        /// </summary>
        public RoadNode NewlyCreatedNode { get; set; }
        public RoadNode Reference { get; set; }

        //Preview variables
        public ObjPos PrePosition { get; set; }
        public int laneCount = 1;

        void ITool.Draw(GameTime gameTime) {
            if (NewlyCreatedNode == null && Reference == null) return;
            var laneWidth = menu.configuration.LaneSpec.Width;
            var laneColor = menu.configuration.LaneSpec.Color;
            var frame = PrePosition.CalcReferenceFrame();
            var length = frame.Z;
            var width = frame.X * laneWidth * laneCount;
            var height = frame.Y * 0.01f;
            var startPoint = frame.O + height;
            var startPoint2 = startPoint + height;
            var tw = frame.X * 0.1f;
            var bw = frame.X * 0.2f;
            var rl = frame.Z * 5;
            var quad = new QuadOld(
                new VertexPositionColorTexture(startPoint + length        , laneColor, new(0        , 0)),
                new VertexPositionColorTexture(startPoint + length + width, laneColor, new(laneCount, 0)),
                new VertexPositionColorTexture(startPoint          + width, laneColor, new(laneCount, 1)),
                new VertexPositionColorTexture(startPoint                 , laneColor, new(0        , 1))
            );
            var quad2 = new QuadOld(
                new VertexPositionColorTexture(startPoint2 - tw + rl, Color.Orange, new(0, 0)),
                new VertexPositionColorTexture(startPoint2 + tw + rl, Color.Orange, new(1, 0)),
                new VertexPositionColorTexture(startPoint2 + bw     , Color.Orange, new(1, 1)),
                new VertexPositionColorTexture(startPoint2 - bw     , Color.Orange, new(0, 1))
            );
            Mesh bin = menu.renderHelper.GetOrCreateRenderBinForced(Assets.Road);
            bin.DrawQuad(quad); bin.DrawQuad(quad2);
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if (button == MouseButton.Right) {
                //Cancel the placement
                Reference = null;
                NewlyCreatedNode = null;
            } else if (NewlyCreatedNode == null && Reference == null) {
                //State: starting state
                var selectedNode = menu.MouseOverRoad?.SelectedRoadNode;
                if (selectedNode == null) {
                    //Select a position
                    var pos = new ObjPos(menu.GroundSelection, 0);
                    NewlyCreatedNode = new RoadNode(menu.World, "", PrePosition);
                } else {
                    //Select a node
                    Reference = selectedNode;
                }
            } else {
                //Ready to place: selected reference or newly created node
                var n = NewlyCreatedNode ?? new RoadNode(menu.World, "", PrePosition);
                Generator.GenerateLanes(laneCount, n, menu.configuration.LaneSpec   );
                n.PositionProp.Value = PrePosition;
                menu.World.Nodes.data.Add(n);
                NewlyCreatedNode = null;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            if (key >= Keys.D1 && key <= Keys.D9) laneCount = key - Keys.D0;
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            var selectedPosition = menu.GroundSelection;
            if(menu.IsSnapEnabled) selectedPosition = menu.configuration.SnapGrid.Snap(selectedPosition);

            if (NewlyCreatedNode != null) {
                //Orient the node towards the mouse
                var pp = NewlyCreatedNode.PositionProp.Value;
                var orientationVector = selectedPosition - pp.Position;
                var yaw = MathF.Atan2(orientationVector.X, orientationVector.Z);
                pp.Tilt = 0;
                if(!float.IsNaN(yaw)) pp.Azimuth = GeometryUtils.RadiansToField(yaw);
                pp.Inclination = 0;
                PrePosition = pp;
            } else {

                var pp = Reference?.PositionProp?.Value ?? ObjPos.Zero;
                pp.Position = selectedPosition;
                PrePosition = pp;
            }
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showSnapOptions);
        }
    }
}
