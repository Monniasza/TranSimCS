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

        string ITool.Description => (node == null) ? "Select a road node end to create a lane strip"
            : "LMB on a segment end or road node end to build a segment, or RMB to cancel";

        LaneEnd? node;
        public LaneStrip? SegmentAlreadyExists { get; private set; } = null;

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var newNode = menu.MouseOverRoad?.SelectedLaneEnd;
                if (node == null) {
                    node = newNode;
                    Debug.Print($"Selected node: {newNode}");
                } else if(newNode != null){
                    var segment = menu.world.GetOrMakeRoadStrip(node.Value.RoadNodeEnd, newNode.Value.RoadNodeEnd);
                    var strip = new LaneStrip(segment, node.Value, newNode.Value);
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
                var node0 = node.Value;
                var lane0 = node0.lane;

                //Initial placeholders for new values
                var endLeftPos = Vector3.Zero;
                var endRightPos = Vector3.Zero;
                var endingTangent = Vector3.Zero;

                var startingPosition0 = Geometry.calcLineEnd(node0.RoadNodeEnd, lane0.LeftPosition);
                var startingTangent = startingPosition0.Tangential;
                var startLeftPos = startingPosition0.Position;
                var startRightPos = startLeftPos + (startingPosition0.Lateral * lane0.Width);
                if (node0.end == NodeEnd.Backward) {
                    var shift = lane0.Width * startingPosition0.Lateral;
                    startLeftPos -= shift;
                    startRightPos -= shift;
                }

                //Calculate the new values
                var mouseOverLaneEnd = menu.MouseOverRoad?.SelectedLaneEnd;
                var mouseOverLane = mouseOverLaneEnd?.lane;
                if (mouseOverLaneEnd == null) {
                    //Create a synthetic end
                    SegmentAlreadyExists = null;
                    var groundPlane = new Plane(0, 1, 0, 0);
                    endLeftPos = Geometry.IntersectRayPlane(menu.MouseRay, groundPlane);
                    var reflectionVector = endLeftPos - startLeftPos;
                    reflectionVector = new(reflectionVector.Z, reflectionVector.Y, -reflectionVector.X);
                    reflectionVector.Normalize();
                    endingTangent = Geometry.ReflectVectorByNormal(startingTangent, reflectionVector);
                    Vector3 endingLateral = new(endingTangent.Z, endingTangent.Y, -endingTangent.X);
                    endRightPos = endLeftPos + (endingLateral * node.Value.lane.Width);
                } else {
                    var mouseOverNode = mouseOverLane.RoadNode;
                    var mouseOverNodeEnd = mouseOverLaneEnd.Value.RoadNodeEnd;
                    var lend = Geometry.calcLineEnd(mouseOverNodeEnd, mouseOverLane.LeftPosition);
                    var rend = Geometry.calcLineEnd(mouseOverNodeEnd, mouseOverLane.RightPosition);
                    endingTangent = lend.Tangential;
                    endLeftPos = lend.Position;
                    endRightPos = rend.Position;
                    if(node0.end == NodeEnd.Backward) {
                        var tmp = endLeftPos;
                        endLeftPos = endRightPos;
                        endRightPos = tmp;
                    }
                    SegmentAlreadyExists = menu.world.FindLaneStrip(node.Value, mouseOverLaneEnd.Value);
                }

                //Draw the preview
                Color previewColor = lane0.Spec.Color;
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
