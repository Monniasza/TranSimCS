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
using TranSimCS.Menus.Gizmo;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Spline;
using TranSimCS.Worlds;
using static MLEM.Ui.Elements.Paragraph;

namespace TranSimCS.Menus.InGame {
    public interface ITool {
        public string Name { get; }
        public string Description { get; }
        public void OnClick(MouseButton button);
        public void OnRelease(MouseButton button);
        public void OnKeyDown(Keys key);
        public void OnKeyUp(Keys key);
        public void Update(GameTime gameTime);
        public void Draw(GameTime gameTime);
        public void Draw2D(GameTime gameTime);
        public void AddSelectors(MultiMesh addTo) { }
    }

    public class RoadDemolitionTool(InGameMenu game) : ITool {
        string ITool.Name => "Road Demolition Tool";

        string ITool.Description => "LMB to demolish the selected road segment, RMB to demolish only the selected lane";

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public void Update(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            RoadSelection MouseOverRoad = game.MouseOverRoad;
            World world = game.World;

            //Demolish the selected road segment if the left mouse button is clicked
            if (button == MouseButton.Left) {
                // If a road segment is selected, remove it from the world
                var selectedRoad = MouseOverRoad?.SelectedLaneTag?.road;
                if (selectedRoad != null) {
                    Debug.Print($"Demolishing road segment: {selectedRoad}");
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

    public class RoadCreationTool(InGameMenu menu): ITool {
        static readonly Vector3 offset = new Vector3(0, 0.01f, 0);

        string ITool.Name => "Road creation tool";

        string ITool.Description => (node == null) ? "Select a road node end to create a lane strip. Ctrl to connect inline"
            : "LMB on a segment end or road node end to build a segment, or RMB to cancel. 123456789 to set number of lanes, 0 for all. Ctrl to connect with the end.";

        LaneEnd? node;
        public LaneStrip? SegmentAlreadyExists { get; private set; } = null;
        public ObjPos? NewNodePosition { get; private set; }

        private LaneEnd? GetLaneEnd() {
            var le = menu.MouseOverRoad?.SelectedLaneEnd;
            var segment = menu.MouseOverRoad?.SelectedLaneStrip;
            if (le == null) return null;
            var le1 = le.Value;
            if (menu.Game.KeyboardState.IsKeyDown(Keys.LeftControl)) le1 = le1.OppositeEnd;
            if (node != null) return le1.OppositeEnd;
            return le1;
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var selectedNode = GetLaneEnd();
                if (menu.SelectedObject is AddLaneSelection als) {
                    //The user wants to create a new lane
                    var newLaneEnd = als.NewLane(menu.roadProperty.Value);
                    selectedNode = newLaneEnd;
                }
                if (node == null) {
                    node = selectedNode;
                    Debug.Print($"Selected node: {selectedNode}");
                } else {
                    var world = node.Value.lane.RoadNode.World;
                    var spec = node.Value.lane.Spec;
                    if (selectedNode == null && NewNodePosition != null) {
                        //Create a new node
                        var newNode = new RoadNode(world, "", NewNodePosition.Value);
                        var newLane = new Lane(newNode);
                        newLane.Spec = spec;
                        newLane.LeftPosition = 0;
                        newLane.RightPosition = node.Value.lane.Width;
                        selectedNode = newLane.Front;
                        newNode.AddLane(newLane);
                        world.RoadNodes.Add(newNode);
                    }
                    if(selectedNode != null) {
                        var segment = world.GetOrMakeRoadStrip(node.Value.RoadNodeEnd, selectedNode.Value.RoadNodeEnd);
                        var strip = new LaneStrip(segment, node.Value, selectedNode.Value.OppositeEnd);
                        strip.Spec = spec;
                        segment.MaybeAddLaneStrip(strip);
                        node = selectedNode;
                    }
                }
            } else if(button == MouseButton.Right) {
                node = null;
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

        public void Update(GameTime gameTime) {
            //unused
        }

        public void Draw(GameTime gameTime) {
            //Draw the preview of the road segment
            if(node != null) {
                var node0 = node.Value;
                var lane0 = node0.lane;

                var startingPosition0 = Geometry.calcLineEnd(node0.RoadNodeEnd, lane0.MiddlePosition);
                var startingTangent = startingPosition0.Tangential;
                var startingLateral = startingPosition0.Lateral;
                var startPos = startingPosition0.Position;
                var startWidth = lane0.Width;

                //Initial placeholders for new values
                var endPos = Vector3.Zero;
                var endTangent = Vector3.Zero;
                var endLateral = Vector3.Zero;
                var endWidth = startWidth;

                //Calculate the new values
                var mouseOverLaneEnd = GetLaneEnd();
                var mouseOverLane = mouseOverLaneEnd?.lane;

                if (menu.SelectedObject is AddLaneSelection als) {
                    //The user wants to create a new lane
                    var mouseOverNodeEnd = als.nodeEnd;
                    endWidth = menu.roadProperty.Value.Width;
                    var range = als.CalculateOffset(endWidth/2);
                    var end = Geometry.calcLineEnd(mouseOverNodeEnd, range);
                    endTangent = end.Tangential;
                    endPos = end.Position;
                    endLateral = end.Lateral;
                    SegmentAlreadyExists = null;
                    NewNodePosition = null;
                } else if (mouseOverLaneEnd == null) {
                    //Create a synthetic end
                    var isSameDirection = menu.CheckSameDirection.Checked;
                    SegmentAlreadyExists = null;
                    endPos = menu.GroundSelection;

                    if (isSameDirection) {
                        endTangent = startingTangent;
                        endLateral = startingLateral;
                    } else {
                        var reflectionVector = endPos - startPos;
                        reflectionVector = new(reflectionVector.Z, reflectionVector.Y, -reflectionVector.X);
                        reflectionVector.Normalize();
                        endTangent = Geometry.ReflectVectorByNormal(startingTangent, reflectionVector);
                        endLateral = -Geometry.ReflectVectorByNormal(startingLateral, reflectionVector);
                    }

                    Vector3 endingLateral = new(endTangent.Z, endTangent.Y, -endTangent.X);
                    var endLeftPos = endPos - (endingLateral * node.Value.lane.Width / 2);
                    var tilt = node.Value.lane.RoadNode.PositionProp.Value.Tilt;
                    //Calculate the NodePosition
                    NewNodePosition = ObjPos.FromPosTangentTilt(endLeftPos, endTangent, tilt);
                } else {
                    //Take an existing end
                    var mouseOverNodeEnd = mouseOverLaneEnd.Value.RoadNodeEnd;
                    var end = Geometry.calcLineEnd(mouseOverNodeEnd, mouseOverLane.MiddlePosition);
                    endTangent = end.Tangential;
                    endLateral = end.Lateral;
                    endPos = end.Position;
                    endWidth = mouseOverLane.Width;
                    SegmentAlreadyExists = menu.World.FindLaneStrip(node.Value, mouseOverLaneEnd.Value);
                    NewNodePosition = null;
                }

                //Draw the preview
                Color previewColor = lane0.Spec.Color;
                previewColor.A = 100;
                if (SegmentAlreadyExists != null) previewColor = Color.Red;
                var startDiff = (startingLateral * startWidth) / 2;
                var endDiff = (endLateral * endWidth) / 2;
                Bezier3 lbound = Geometry.GenerateJoinSpline(startPos - startDiff, endPos - endDiff, startingTangent, -endTangent) + offset;
                Bezier3 rbound = Geometry.GenerateJoinSpline(startPos + startDiff, endPos + endDiff, startingTangent, -endTangent) + offset;
                IRenderBin renderBin = menu.renderHelper.GetOrCreateRenderBin(InGameMenu.roadTexture);
                RoadRenderer.DrawBezierStrip(lbound, rbound, renderBin, previewColor);
            }
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.AddSelectors(MultiMesh addTo) {
            SelectionUtils.AddAddLaneSelectors(menu);
        }
    }

    public class MoveTool(InGameMenu game) : ITool {
        string ITool.Name => "Move nodes and objects";

        string ITool.Description => "Drag the object with the left mouse button to move it.";

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

        void ITool.AddSelectors(MultiMesh addTo) {
            //Add azimuth gizmos
            var renderBin = addTo.GetOrCreateRenderBin(InGameMenu.addTexture);

            foreach (var roadNode in game.World.RoadNodes) {
                var azimuthGizmo = new AzimuthGizmo(roadNode);
                azimuthGizmo.CreateMesh(renderBin);
            }
            
        }
    }

    public class PickerTool(InGameMenu game) : ITool {
        string ITool.Name => "Lane spec picker";

        string ITool.Description => "Left click near a node to select its lane spec, or in the middle of a lane strip for the lane strip's spec";

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
                game.roadProperty.Value = spec.Value;
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

        string ITool.Description => (NewlyCreatedNode != null) ? "Click to set direction of the newly built node"
            : (Reference == null) ? "123456789 to set number of lanes, click on a node to set direction from it, click elsewhere to set direction manually"
            : "Click to place a node. The reference will not be reset after placement";

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
            var frame = PrePosition.CalcReferenceFrame();
            var length = frame.Z;
            var width = frame.X * 3.5f * laneCount;
            var height = frame.Y * 0.01f;
            var startPoint = frame.O + height;
            var startPoint2 = startPoint + height;
            var tw = frame.X * 0.1f;
            var bw = frame.X * 0.2f;
            var rl = frame.Z * 5;
            var quad = new Quad(
                new VertexPositionColorTexture(startPoint + length        , Color.Gray, new(0        , 0)),
                new VertexPositionColorTexture(startPoint + length + width, Color.Gray, new(laneCount, 0)),
                new VertexPositionColorTexture(startPoint          + width, Color.Gray, new(laneCount, 1)),
                new VertexPositionColorTexture(startPoint                 , Color.Gray, new(0        , 1))
            );
            var quad2 = new Quad(
                new VertexPositionColorTexture(startPoint2 - tw + rl, Color.Orange, new(0, 0)),
                new VertexPositionColorTexture(startPoint2 + tw + rl, Color.Orange, new(1, 0)),
                new VertexPositionColorTexture(startPoint2 + bw     , Color.Orange, new(1, 1)),
                new VertexPositionColorTexture(startPoint2 - bw     , Color.Orange, new(0, 1))
            );
            IRenderBin bin = menu.renderHelper.GetOrCreateRenderBin(InGameMenu.roadTexture);
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
                Generator.GenerateLanes(laneCount, n);
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
                if(!float.IsNaN(yaw)) pp.Azimuth = Geometry.RadiansToField(yaw);
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

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var laneSpec = game.roadProperty.Value;
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

        public string Description => throw new NotImplementedException();

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
