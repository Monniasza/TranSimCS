using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.AccessControl;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MLEM.Ui;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public class RoadPlan {
        public Vector3 startTangent;
        public Vector3 startPos;
        public Vector3 startLateral;

        public Vector3 endTangent;
        public Vector3 endPos;
        public Vector3 endLateral;
    }
    public interface RoadMode {
        public string Name { get; }
        public void CreateValues(RoadPlan plan);
    }

    public class StraightMode : RoadMode {
        public string Name => "Straight";
        public void CreateValues(RoadPlan plan) {
            plan.endLateral = plan.startLateral;
            plan.endTangent = plan.startTangent;
            Ray ray = new Ray(plan.startPos, plan.startTangent);
            var endPos = Geometry.FindNearest(ray, plan.endPos, out var discard);
            plan.endPos = endPos;
        }
    }
    public class SBendMode: RoadMode {
        public string Name => "S-bend, same-direction";
        public void CreateValues(RoadPlan plan) {
            plan.endLateral = plan.startLateral;
            plan.endTangent = plan.startTangent;
        }
    }

    public class CircMode: RoadMode {
        public string Name => "Circular arc";
        public void CreateValues(RoadPlan plan) {
            var reflectionVector = plan.endPos - plan.startPos;
            reflectionVector = new(reflectionVector.Z, reflectionVector.Y, -reflectionVector.X);
            reflectionVector.Normalize();
            plan.endTangent = Geometry.ReflectVectorByNormal(plan.startTangent, reflectionVector);
            plan.endLateral = -Geometry.ReflectVectorByNormal(plan.startLateral, reflectionVector);
        }
    }

    public interface ChainMode: IEquatable<ChainMode> {
        public string Name { get; }
        public LaneSpec ChainValues(InGameMenu game);
        bool IEquatable<ChainMode>.Equals(ChainMode other) {
            return this == other;
        }
    }
    public class ChainModeChained: ChainMode {
        public static ChainModeChained value = new ChainModeChained();
        private ChainModeChained() { }
        public string Name => "From previous";
        public LaneSpec ChainValues(InGameMenu game) =>
            game.RoadCreationTool.node?.lane?.Spec
            ?? ChainModeCustom.value.ChainValues(game);
    }
    public class ChainModeCustom : ChainMode {
        public static ChainModeCustom value = new ChainModeCustom();
        private ChainModeCustom() { }
        public string Name => "Custom from road configurator";
        public LaneSpec ChainValues(InGameMenu game) => game.configurator.laneSpecProp.Value;
    }

    public class RoadCreationTool: ITool {
        public static readonly ChainMode chained = ChainModeChained.value;
        public static readonly ChainMode custom = ChainModeCustom.value;
        static readonly Vector3 offset = new Vector3(0, 0.01f, 0);

        string ITool.Name => "Road creation tool";

        string ITool.Description => (node == null) ? "Select a road node end to create a lane strip. Ctrl to connect inline"
            : "LMB on a segment end or road node end to build a segment, or RMB to cancel. 123456789 to set number of lanes, 0 for all. Ctrl to connect with the end.";

        public (object[], string)[] PromptKeys() {
            (object[], string) countPrompt = ([Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9], " to set number of lanes");
            List<(object[], string)> keys = [countPrompt];
            keys.Add(([Keys.LeftControl], "to connect inline"));
            if (node == null) {
                keys.Add(([MouseButton.Left], "Select a road node end to create a lane strip."));
                keys.Add(([MouseButton.Left], "elsewhere to set direction manually"));
            } else {
                keys.Add(([MouseButton.Right], "to cancel"));
            }
            return keys.ToArray();
        }

        public LaneEnd? node { get; set; }
        public LaneStrip? SegmentAlreadyExists { get; private set; } = null;
        public ObjPos? NewNodePosition { get; private set; }
        public RoadMode Mode { get; set; } = new CircMode();

        public readonly InGameMenu menu;
        public RoadCreationTool(InGameMenu menu) {
            this.menu = menu;
            RoadTools = new RoadTools(menu, Anchor.CenterLeft, new(200, 0.5f));
        }

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
                    var spec = RoadTools.ChainMode.Value.ChainValues(menu);
                    selectedNode = newLaneEnd;
                    newLaneEnd.lane.Spec = spec;
                }
                if (node == null) {
                    node = selectedNode;
                    Debug.Print($"Selected node: {selectedNode}");
                } else {
                    var world = node.Value.lane.RoadNode.World;
                    var spec = RoadTools.ChainMode.Value.ChainValues(menu);
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
                        var strip = world.GetOrMakeLaneStrip(node.Value, selectedNode.Value.OppositeEnd);
                        strip.Spec = spec;
                        node = selectedNode;
                    }
                }
            } else if(button == MouseButton.Right) {
                node = null;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            switch (key) {
                case Keys.PageUp:
                    RoadTools.Height.Value += RoadTools.HeightStep.Value;
                    break;
                case Keys.PageDown:
                    RoadTools.Height.Value -= RoadTools.HeightStep.Value;
                    break;
            }
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
                    SegmentAlreadyExists = null;
                    Plane selectionPlane = new Plane(Vector3.UnitY * RoadTools.Height.Value, Vector3.UnitY);
                    endPos = Geometry.IntersectRayPlane(menu.MouseRay, selectionPlane);

                    RoadPlan plan = new RoadPlan {
                        startLateral = startingLateral,
                        endLateral = endLateral,
                        startPos = startPos,
                        endPos = endPos,
                        startTangent = startingTangent,
                        endTangent = endTangent,
                    };

                    //Apply the road mode
                    Mode.CreateValues(plan);

                    endTangent = plan.endTangent;
                    endLateral = plan.endLateral;
                    endPos = plan.endPos;
                    startingLateral = plan.startLateral;
                    startPos = plan.startPos;
                    startingTangent = plan.startTangent;

                    Vector3 endingLateral = new(endTangent.Z, endTangent.Y, -endTangent.X);
                    var endLeftPos = endPos - (endingLateral * node.Value.lane.Width / 2);
                    var tilt = node.Value.lane.RoadNode.PositionProp.Value.Tilt;

                    //Flatten tilt or inclination
                    //Calculate the NodePosition
                    var newNodePosition = ObjPos.FromPosTangentTilt(endLeftPos, endTangent, tilt);
                    if (RoadTools.flattenTilt.Checked) newNodePosition.Tilt = 0;
                    if (RoadTools.flattenIncline.Checked) newNodePosition.Inclination = 0;
                    NewNodePosition = newNodePosition;
                    var frame = newNodePosition.CalcReferenceFrame();
                    endTangent = frame.Z;
                    endLateral = frame.X;
                    
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

        public RoadTools RoadTools { get; private set; }
        public const string uiID = "roadTools";
        void ITool.OnOpen() {
            menu.UiSystem.Add(uiID, RoadTools);
        }
        void ITool.OnClose() {
            menu.UiSystem.Remove(uiID);
        }
    }
}
