using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.Gizmo;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Worlds;
using static MLEM.Ui.Elements.Paragraph;

namespace TranSimCS.Tools {
    public interface ITool {
        public string Name { get; }
        public string Description { get; }
        public void OnClick(MouseButton button) { }
        public void OnRelease(MouseButton button) { }
        public void OnKeyDown(Keys key) { }
        public void OnKeyUp(Keys key) { }
        public void Update(GameTime gameTime);
        public void Draw(GameTime gameTime);
        public void Draw2D(GameTime gameTime);
        public void AddSelectors(MultiMesh invisibleSelectors, MultiMesh visibleSelectors) { }
        public (object[], string)[] PromptKeys();

        public void OnOpen() { }
        public void OnClose() { }
    }

    public class RoadDemolitionTool(InGameMenu game) : ITool {

        private static Logger log = LogManager.GetCurrentClassLogger();
        string ITool.Name => "Road Demolition Tool";

        string ITool.Description => "Demolish objects and subcomponents";

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "to demolish the road segment, a node or the entire object"),
            ([MouseButton.Right], "to demolish the lane, a lane strip or a subcomponent")
        ];

        public void Update(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            RoadSelection MouseOverRoad = game.MouseOverRoad;
            TSWorld world = game.World;

            //Demolish the selected road segment if the left mouse button is clicked
            if (button == MouseButton.Left) {
                // If a road segment is selected, remove it from the world
                var selectedRoad = MouseOverRoad?.SelectedLaneTag?.road;
                var selectedNode = MouseOverRoad?.SelectedRoadNode;
                if (selectedNode != null) {
                    //Demolish a node
                    MouseOverRoad = null;
                    world.RoadNodes.Remove(selectedNode);
                } else if (selectedRoad != null) {
                    log.Trace($"Demolishing road segment: {selectedRoad}");
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.Remove(selectedRoad); // Remove the selected road segment from the world
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (button == MouseButton.Right) {
                // If a lane tag is selected, remove it from the road segment
                var selectedLaneStrip = MouseOverRoad?.SelectedLaneStrip;
                var selectedNode = MouseOverRoad?.SelectedRoadNode;
                var selectedRoadHalf = MouseOverRoad?.SelectedRoadHalf;
                var selectedLane = MouseOverRoad?.SelectedLane;
                if(selectedLane != null) {
                    //Demolish the node lane
                    MouseOverRoad = null; // Reset the mouse over road selection
                    selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                }else if(selectedLaneStrip != null) {
                    //Demolish just the lane strip
                    MouseOverRoad = null;
                    selectedLaneStrip.Destroy();
                }
            }

            game.MouseOverRoad = MouseOverRoad;
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }
    }

    public class MoveTool(InGameMenu game) : ITool {
        string ITool.Name => "Move nodes and objects";

        string ITool.Description => "Drag and rotate objects";

        public Vector3? DragFrom { get; private set; }
        public IDraggableObj ObjToDrag { get; private set; }

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            //unused
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            var gs = game.GroundSelection;
            if (game.Game.MouseState.LeftButton == ButtonState.Pressed) {
                if(game.Game.MouseStateOld.LeftButton == ButtonState.Released) {
                    //Object newly clicked
                    //ObjToDrag = game.MouseOverRoad?.SelectedRoadNode;
                    var candidate = game.SelectedObject;
                    if (candidate is IDraggableObj drag) ObjToDrag = drag;
                } else if (DragFrom != null) {
                    //Object is held
                    var dragFrom = DragFrom.Value;
                    var delta = gs - dragFrom;
                    ObjToDrag?.Drag(delta, dragFrom);
                }
                DragFrom = gs;
            } else {
                ObjToDrag = null;
                DragFrom = null;
            }
        }

        void ITool.AddSelectors(MultiMesh addTo, MultiMesh visibleSelectors) {
            //Add azimuth gizmos
            var renderBin = addTo.GetOrCreateRenderBin(Assets.Add);

            foreach (var roadNode in game.World.RoadNodes) {
                var azimuthGizmo = new AzimuthGizmo(roadNode);
                azimuthGizmo.CreateMesh(renderBin);
            }

        }

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left, SpecialKey.MouseMove], "to move the object"),
            ([MouseButton.Right, SpecialKey.MouseMove], "to rotate the object")
        ];
    }

    public class PickerTool(InGameMenu game) : ITool {
        string ITool.Name => "Lane spec picker";

        string ITool.Description => "Pick lane specs";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], " near a node to select its lane spec"),
            ([MouseButton.Left], " in the middle of a lane strip for the lane strip's spec")
        ];

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var selection = game.MouseOverRoad;
                var laneSpec = selection?.SelectedLaneStrip?.Spec;
                var nodeSpec = selection?.SelectedLane?.Spec;
                var spec = nodeSpec ?? laneSpec;
                if (spec == null) return;
                game.configuration.LaneSpec = spec.Value;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            //unused
        }
    }

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
            var quad = new Quad(
                new VertexPositionColorTexture(startPoint + length        , laneColor, new(0        , 0)),
                new VertexPositionColorTexture(startPoint + length + width, laneColor, new(laneCount, 0)),
                new VertexPositionColorTexture(startPoint          + width, laneColor, new(laneCount, 1)),
                new VertexPositionColorTexture(startPoint                 , laneColor, new(0        , 1))
            );
            var quad2 = new Quad(
                new VertexPositionColorTexture(startPoint2 - tw + rl, Color.Orange, new(0, 0)),
                new VertexPositionColorTexture(startPoint2 + tw + rl, Color.Orange, new(1, 0)),
                new VertexPositionColorTexture(startPoint2 + bw     , Color.Orange, new(1, 1)),
                new VertexPositionColorTexture(startPoint2 - bw     , Color.Orange, new(0, 1))
            );
            IRenderBin bin = menu.renderHelper.GetOrCreateRenderBin(Assets.Road);
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
                menu.World.RoadNodes.Add(n);
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
    }

    public class PaintTool(InGameMenu game) : ITool {
        string ITool.Name => "Paint lane specs";

        string ITool.Description => "Click on roads to set their lane specs";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "on roads to set their lane specs"),
        ];

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var laneSpec = game.configuration.LaneSpec;
                var selection = game.MouseOverRoad;
                var lane = selection?.SelectedLane;
                if(lane != null) lane.Spec = laneSpec;
                var strip = selection?.SelectedLaneStrip;
                if (strip != null) strip.Spec = laneSpec;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            //unused
        }
    }

    public class EditNodeTool : ITool {
        public string Name => "Edit road nodes";

        public string Description => "";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "dummy"),
        ];

        public void Draw(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public void Draw2D(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public void OnClick(MouseButton button) {
            throw new NotImplementedException();
        }

        public void OnKeyDown(Keys key) {
            throw new NotImplementedException();
        }

        public void OnKeyUp(Keys key) {
            throw new NotImplementedException();
        }

        public void OnRelease(MouseButton button) {
            throw new NotImplementedException();
        }

        public void Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}
