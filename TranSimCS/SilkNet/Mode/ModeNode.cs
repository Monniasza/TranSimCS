using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Roads.Node;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet.Mode {
    public class ModeNode(SilkNetTest menu) : IMode {
        // Set if the new node has to be rotated after placement
        public RoadNode? NewlyCreatedNode { get; set; }
        // Position reference for pre-oriented nodes
        public IPosition? Reference { get; set; }
        // Position of the current state
        public PositionEulerAngles PrePosition { get; set; }

        string IMode.Title() => "Add road nodes";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 yellow = new(1, 1, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            Vector4 magenta = new(0.5f, 0, 0.5f, 1);
            if (ImGui.Begin("Road node options")) {
                if(NewlyCreatedNode != null) {
                    //Node to be oriented
                    ImGui.Text("Point and [LMB] to set the direction of the road node. [RMB to cancel]");
                }else if(Reference == null) {
                    //Node not yet created
                    ImGui.Text("[LMB] on a positioned object to set direction from it, [LMB] elsewhere to set direction manually");
                    var nextReference = menu.MouseOver?.As<IPosition>();
                    if(menu.LeftLanes + menu.RightLanes <= 0) {
                        ImGui.TextColored(magenta, "No lanes to create! Increse the lane count on any side with [Q/P], or [RMB] to cancel.");
                    }else if (nextReference is IPosition) {
                        ImGui.TextColored(green, "The road node will already have its orientation set.");
                        ImGui.TextColored(green, "More road nodes can be created without having to pick a reference.");
                    } else if (nextReference != null) {
                        ImGui.TextColored(red, "The selected object is not a valid position reference.");
                        ImGui.TextColored(red, "The road node will not have its orientation set.");
                        ImGui.TextColored(red, "[LMB] to start placing. After placement, orient it.");
                    } else {
                        ImGui.TextColored(maroon, "No position reference picked.");
                        ImGui.TextColored(maroon, "The road node will not have its orientation set.");
                        ImGui.TextColored(maroon, "[LMB] to start placing. After placement, orient it.");
                    }
                    ImGui.TextColored(yellow, "[Q/E] to change left lanes. [O/P] to change right lanes.");
                    ImGui.TextColored(yellow, "[Q/P] to increase lanes. [E/O] to decrease lanes.");
                    ImGui.TextColored(yellow, "[Q/O] to move the edge left. [E/P] to move the edges right.");
                } else {
                    //Node with a preset orientation
                    ImGui.Text("[LMB] to place one or more nodes. [RMB to cancel]");
                }
                menu.ShowNodeCreator();
                menu.ShowLaneCreator();
                menu.ShowSnappingSettings();
                ImGui.End();
            }
            
        }
        void IMode.OnKeyPress(Key key) {
        switch(key) {
            case Key.Q:
                menu.LeftLanes++;
                break;
            case Key.E:
                if (menu.LeftLanes > 0) menu.LeftLanes--;
                break;
            case Key.O:
                if (menu.RightLanes > 0) menu.RightLanes--;
                break;
            case Key.P:
                menu.RightLanes++;
                break;
            }
        }
        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            if (NewlyCreatedNode == null && Reference == null) return;
            var laneWidth = menu.LaneSpec.Width;
            var laneColor = menu.LaneSpec.Color;
            var frame = PrePosition.CalcReferenceFrame();
            var length = frame.Z;
            int leftLanes = menu.LeftLanes;
            int rightLanes = menu.RightLanes;
            float medianWidth = menu.MedianWidth;
            var widthL = frame.X * laneWidth * leftLanes;
            var widthR = frame.X * laneWidth * rightLanes;
            var height = frame.Y * 0.01f;
            var startPointL = frame.O + height - frame.X * 0.5f * medianWidth;
            var startPointR = frame.O + height + frame.X * 0.5f * medianWidth;

            //Generate lanes
            Mesh bin = renderMeshPool.GetOrCreateRenderBinForced(Assets.Road);
            bin.DrawParallelogram(startPointR, widthR, length, laneColor, new(0, 0, rightLanes, 1));
            bin.DrawParallelogram(startPointL, -widthL, -length, laneColor, new(0, 0, leftLanes, 1));

            //Generate front and back markers
            var front = frame.O + frame.Z * 2;
            var back = frame.O - frame.Z * 2;
            bin.DrawLine(frame.O, front, frame.Y, Colors.Red);
            bin.DrawLine(frame.O, back, frame.Y, Colors.Maroon);
        }
        void IMode.OnMousePress(MouseButton button) {
            var refplane = menu.snappingGrid.CreateSnappingPlane();
            if (button == MouseButton.Right) {
                //Cancel the placement
                Reference = null;
                NewlyCreatedNode = null;
                return;
            }
            if (menu.LeftLanes + menu.RightLanes == 0) return;
            if (NewlyCreatedNode == null && Reference == null) {
                //State: starting state
                if (menu.MouseOver?.SelectedObj is IPosition selectedNode) {
                    //Select a node
                    Reference = selectedNode;
                } else {
                    //Select a position
                    NewlyCreatedNode = new RoadNode("", PrePosition);
                }
            } else {
                //Ready to place: selected reference or newly created node
                var n = NewlyCreatedNode ?? new RoadNode("", PrePosition);
                int leftLanes = menu.LeftLanes;
                int rightLanes = menu.RightLanes;
                var medianWidth = menu.MedianWidth;
                Generator.GenerateLanes(leftLanes, rightLanes, medianWidth, n, menu.LaneSpec);
                n.PositionProp.Value = PrePosition;
                menu.World.Nodes.data.Add(n);
                NewlyCreatedNode = null;
            }
        }
        void IMode.Update(double dt) {
            var refplane = menu.snappingGrid.CreateSnappingPlane();
            var selectedPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, refplane);
            if(menu.SnappingEnabled) selectedPosition = menu.snappingGrid.Snap(selectedPosition);

            if (NewlyCreatedNode != null) {
                //Orient the node towards the mouse
                var pp = NewlyCreatedNode.PositionProp.Value;
                var orientationVector = selectedPosition - pp.Position;
                var yaw = MathF.Atan2(orientationVector.X, orientationVector.Z);
                pp.Tilt = 0;
                if (!float.IsNaN(yaw)) pp.Azimuth = GeometryUtils.RadiansToField(yaw);
                pp.Inclination = 0;
                PrePosition = pp;
            } else if(menu.LeftLanes + menu.RightLanes > 0){
                var pp = Reference?.PositionProp?.Value ?? PositionEulerAngles.Zero;
                pp.Position = selectedPosition;
                PrePosition = pp;
            }
        }
    }
}
