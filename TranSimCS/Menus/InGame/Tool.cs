using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Roads;

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
            World world = game.world;

            //Demolish the selected road segment if the left mouse button is clicked
            if (button == MouseButton.Left) {
                // If a road segment is selected, remove it from the world
                if (MouseOverRoad != null) {
                    Debug.Print($"Demolishing road segment: {MouseOverRoad.SelectedLaneTag.road}");
                    var tbremove = MouseOverRoad.SelectedLaneTag.road; // Get the road segment to remove
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.Remove(tbremove); // Remove the selected road segment from the world
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (button == MouseButton.Right) {
                // If a lane tag is selected, remove it from the road segment
                if (MouseOverRoad != null) {
                    var selectedRoad = MouseOverRoad.SelectedLaneTag.road; // Get the selected road half
                    var selectedLaneStrip = MouseOverRoad.SelectedLaneStrip; // Get the selected lane tag
                    var selectedNode = selectedRoad.GetHalf(MouseOverRoad.SelectedRoadHalf);// Get the node of the selected road half
                    var selectedLane = selectedLaneStrip.GetHalf(MouseOverRoad.SelectedRoadHalf); // Get the lane number from the selected lane tag 
                    if (MouseOverRoad.SelectedLaneT > 0.3f && MouseOverRoad.SelectedLaneT < 0.7f) {
                        //Demolish just the lane strip
                        Debug.Print($"Demolishing lane strip: {selectedLaneStrip} of segment {selectedRoad.StartNode.Id} to {selectedRoad.EndNode.Id}");
                        MouseOverRoad = null;
                        selectedLaneStrip.Destroy();
                    } else {
                        //Demolish the node lane
                        Debug.Print($"Demolishing lane: {selectedLane} of segment {selectedRoad.StartNode.Id} to {selectedRoad.EndNode.Id}");
                        MouseOverRoad = null; // Reset the mouse over road selection
                        selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                    }
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

        string ITool.Description => (node == null) ? "Select a road node end to create a lane strip"
            : "LMB on a segment end or road node end to build a segment, or RMB to cancel";

        Lane node;
        public LaneStrip? SegmentAlreadyExists { get; private set; } = null;

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var newNode = menu.MouseOverRoad?.SelectedLane;
                if (node == null) {
                    node = newNode;
                    Debug.Print($"Selected node: {newNode}");
                } else if(newNode != null){
                    var segment = menu.world.GetOrMakeRoadStrip(node.RoadNode, newNode.RoadNode);
                    var strip = new LaneStrip(segment, node, newNode);
                    segment.MaybeAddLaneStrip(strip);
                }
            }else if(button == MouseButton.Right) {
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
                //Initial placeholders for new values
                var endLeftPos = Vector3.Zero;
                var endRightPos = Vector3.Zero;
                var endingTangent = Vector3.Zero;

                var startingPosition0 = Geometry.calcLineEnd(node.RoadNode, node.LeftPosition);
                var startingTangent = startingPosition0.Tangential;
                var startLeftPos = startingPosition0.Position;
                var startRightPos = startLeftPos + (startingPosition0.Lateral * node.Width);

                //Calculate the new values
                var mouseOverLane = menu.MouseOverRoad?.SelectedLane;
                if (mouseOverLane == null) {
                    //Create a synthetic end
                    SegmentAlreadyExists = null;
                    var groundPlane = new Plane(0, 1, 0, 0);
                    endLeftPos = Geometry.IntersectRayPlane(menu.MouseRay, groundPlane);
                    var reflectionVector = endLeftPos - startLeftPos;
                    reflectionVector = new(reflectionVector.Z, reflectionVector.Y, -reflectionVector.X);
                    reflectionVector.Normalize();
                    endingTangent = Geometry.ReflectVectorByNormal(startingTangent, reflectionVector);
                    Vector3 endingLateral = new(endingTangent.Z, endingTangent.Y, -endingTangent.X);
                    endRightPos = endLeftPos + (endingLateral * node.Width);
                } else {
                    var mouseOverNode = mouseOverLane.RoadNode;
                    var lend = Geometry.calcLineEnd(mouseOverNode, mouseOverLane.LeftPosition);
                    var rend = Geometry.calcLineEnd(mouseOverNode, mouseOverLane.RightPosition);
                    endingTangent = lend.Tangential;
                    endLeftPos = lend.Position;
                    endRightPos = rend.Position;
                    SegmentAlreadyExists = menu.world.FindLaneStrip(node, mouseOverLane);
                }

                //Draw the preview
                Color previewColor = node.Spec.Color;
                previewColor.A = 100;
                if (SegmentAlreadyExists != null) previewColor = Color.Red;
                Bezier3 lbound = Geometry.GenerateJoinSpline(startLeftPos, endLeftPos, startingTangent, endingTangent) + offset;
                Bezier3 rbound = Geometry.GenerateJoinSpline(startRightPos, endRightPos, startingTangent, endingTangent) + offset;
                IRenderBin renderBin = menu.renderHelper.GetOrCreateRenderBin(InGameMenu.roadTexture);
                RoadRenderer.DrawBezierStrip(lbound, rbound, renderBin, previewColor);
            }
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }
    }

    public class MoveTool(InGameMenu game) : ITool {
        string ITool.Name => "Move nodes and objects";

        string ITool.Description => throw new NotImplementedException();

        RoadNode RoadNode { get; set; }

        void ITool.Draw(GameTime gameTime) {
            throw new NotImplementedException();
        }

        void ITool.Draw2D(GameTime gameTime) {
            throw new NotImplementedException();
        }

        void ITool.OnClick(MouseButton button) {
            throw new NotImplementedException();
        }

        void ITool.OnKeyDown(Keys key) {
            throw new NotImplementedException();
        }

        void ITool.OnKeyUp(Keys key) {
            throw new NotImplementedException();
        }

        void ITool.OnRelease(MouseButton button) {
            throw new NotImplementedException();
        }

        void ITool.Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}
