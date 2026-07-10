using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
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
            List<(object[], string)> keys = [];
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
        public RoadNode? NewlyCreatedNode { get; set; }
        public RoadNode? Reference { get; set; }

        //Preview variables
        public PositionEulerAngles PrePosition { get; set; }
        public readonly AddLaneTools addLaneTools = menu.ToolsPanel.GetPanel<AddLaneTools>(ToolAttribs.showLaneLayout);

        void ITool.Draw(GameTime gameTime) {
            if (NewlyCreatedNode == null && Reference == null) return;
            var laneWidth = menu.configuration.LaneSpec.Width;
            var laneColor = menu.configuration.LaneSpec.Color;
            var frame = PrePosition.CalcReferenceFrame();
            var length = frame.Z;
            int leftLanes = (int)addLaneTools.LeftLaneCount.Value;
            int rightLanes = (int)addLaneTools.RightLaneCount.Value;
            float medianWidth = addLaneTools.MedianWidth.Value;
            var widthL = frame.X * laneWidth * leftLanes;
            var widthR = frame.X * laneWidth * rightLanes;
            var height = frame.Y * 0.01f;
            var startPointL = frame.O + height - (frame.X * 0.5f * medianWidth);
            var startPointR = frame.O + height + (frame.X * 0.5f * medianWidth);

            //Generate lanes
            Mesh bin = menu.renderHelper.GetOrCreateRenderBinForced(Assets.Road);
            bin.DrawParallelogram(startPointR, widthR, length, laneColor, new(0, 0, rightLanes, 1));
            bin.DrawParallelogram(startPointL, -widthL, -length, laneColor, new(0, 0, leftLanes, 1));

            //Generate front and back markers
            var front = frame.O + frame.Z * 2;
            var back = frame.O - frame.Z * 2;
            bin.DrawLine(frame.O, front, frame.Y, Color.Red);
            bin.DrawLine(frame.O, back, frame.Y, Color.Maroon);
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            var refplane = menu.ReferencePlane;
            if (button == MouseButton.Right) {
                //Cancel the placement
                Reference = null;
                NewlyCreatedNode = null;
            } else if (NewlyCreatedNode == null && Reference == null) {
                //State: starting state
                if (menu.MouseOver?.SelectedObj is RoadNode selectedNode) {
                    //Select a node
                    Reference = selectedNode;
                } else {
                    //Select a position
                    var vectorPos = GeometryUtils.IntersectRayPlane(menu.MouseRay, refplane);
                    var pos = new PositionEulerAngles(vectorPos, 0);
                    NewlyCreatedNode = new RoadNode("", PrePosition);
                }
            } else {
                //Ready to place: selected reference or newly created node
                var n = NewlyCreatedNode ?? new RoadNode("", PrePosition);
                int leftLanes = (int)addLaneTools.LeftLaneCount.Value;
                int rightLanes = (int)addLaneTools.RightLaneCount.Value;
                var medianWidth = addLaneTools.MedianWidth.Value;
                Generator.GenerateLanes(leftLanes, rightLanes, medianWidth, n, menu.configuration.LaneSpec);
                n.PositionProp.Value = PrePosition;
                menu.World.Nodes.data.Add(n);
                NewlyCreatedNode = null;
            }
        }

        void ITool.Update(GameTime gameTime) {
            var refplane = menu.ReferencePlane;
            var selectedPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, refplane);
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
                var pp = Reference?.PositionProp?.Value ?? PositionEulerAngles.Zero;
                pp.Position = selectedPosition;
                PrePosition = pp;
            }
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showSnapOptions);
            action.Add(ToolAttribs.showLaneSpecs);
            action.Add(ToolAttribs.showLaneLayout);
        }
    }
}
