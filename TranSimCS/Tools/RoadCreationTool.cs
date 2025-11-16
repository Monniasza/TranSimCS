using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.AccessControl;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MLEM.Ui;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Tools.Panels;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
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
            var endPos = GeometryUtils.FindNearest(ray, plan.endPos, out var _);
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
            var latReflectionVector = new Vector3(reflectionVector.Z, reflectionVector.Y, -reflectionVector.X);
            reflectionVector.Normalize();
            plan.endTangent = -GeometryUtils.ReflectVectorByNormal(plan.startTangent, reflectionVector);
            plan.endLateral = -GeometryUtils.ReflectVectorByNormal(plan.startLateral, latReflectionVector);
        }
    }

    public interface ChainMode: IEquatable<ChainMode?> {
        public string Name { get; }
        public LaneSpec ChainValues(InGameMenu game);
        bool IEquatable<ChainMode?>.Equals(ChainMode? other) {
            return ReferenceEquals(this, other);
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
        private static Logger log = LogManager.GetCurrentClassLogger();

        public static readonly ChainMode chained = ChainModeChained.value;
        public static readonly ChainMode custom = ChainModeCustom.value;
        static readonly Vector3 offset = new Vector3(0, 0.01f, 0);

        string ITool.Name => "Road creation tool";

        string ITool.Description => node == null ? "Pick a lane"
            : "Creating a segment";

        public (object[], string)[] PromptKeys() {
            (object[], string) countPrompt = ([Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9], "to set number of lanes");
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
            RoadTools = menu.ToolsPanel.GetPanel<RoadTools>(ToolAttribs.showRoadTools);
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
                    var newLaneEnd = als.NewLane(menu.configuration.LaneSpec);
                    var spec = RoadTools.ChainMode.Value.ChainValues(menu);
                    selectedNode = newLaneEnd;
                    newLaneEnd.lane.Spec = spec;
                }
                if (node == null) {
                    node = selectedNode;
                    log.Trace($"Selected node: {selectedNode}");
                } else {
                    var world = node.Value.lane.RoadNode.World;
                    var spec = RoadTools.ChainMode.Value.ChainValues(menu);
                    if (selectedNode == null && NewNodePosition != null) {
                        //Create a new node
                        var newNode = new RoadNode(world, "", NewNodePosition.Value);
                        var newLane = new Lane {
                            Spec = spec,
                            LeftPosition = 0,
                            RightPosition = node.Value.lane.Width
                        };
                        selectedNode = newLane.Front;
                        newNode.AddLane(newLane);
                        world.Nodes.data.Add(newNode);
                    }
                    if(selectedNode != null) {
                        var strip = world.GetOrMakeLaneStrip(node.Value, selectedNode.Value.OppositeEnd, menu.configuration.RoadFinish);
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

                var startingPositionRef = LineEnd.calcLineEnd(node0.RoadNodeEnd, lane0.MiddlePosition);
                var startTangent = startingPositionRef.Tangential;
                var startLateral = startingPositionRef.Lateral;
                var startPos = startingPositionRef.Position;
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
                    endWidth = menu.configuration.LaneSpec.Width;
                    var range = als.CalculateOffset(endWidth/2);
                    var end = LineEnd.calcLineEnd(mouseOverNodeEnd, range);
                    endTangent = end.Tangential;
                    endPos = end.Position;
                    endLateral = end.Lateral;
                    SegmentAlreadyExists = null;
                    NewNodePosition = null;
                } else if (mouseOverLaneEnd == null) {
                    //Create a synthetic end
                    SegmentAlreadyExists = null;
                    Plane selectionPlane = new Plane(Vector3.UnitY * RoadTools.Height.Value, Vector3.UnitY);
                    endPos = GeometryUtils.IntersectRayPlane(menu.MouseRay, selectionPlane);

                    RoadPlan plan = new RoadPlan {
                        startLateral = startLateral,
                        endLateral = endLateral,
                        startPos = startPos,
                        endPos = endPos,
                        startTangent = startTangent,
                        endTangent = endTangent,
                    };

                    //Apply the road mode
                    Mode.CreateValues(plan);

                    endTangent = plan.endTangent;
                    endLateral = plan.endLateral;
                    endPos = plan.endPos;
                    startLateral = plan.startLateral;
                    startPos = plan.startPos;
                    startTangent = plan.startTangent;

                    if (RoadTools.flattenTilt.Checked) endLateral = Vector3.Normalize(new Vector3(endLateral.X, 0, endLateral.Z));
                    if (RoadTools.flattenIncline.Checked) endTangent = Vector3.Normalize(new Vector3(endTangent.X, 0, endTangent.Z));
                    var endLeftPos = endPos - endLateral * node.Value.lane.Width / 2;

                    //Flatten tilt or inclination
                    //Calculate the NodePosition
                    var newNodePosition = ObjPos.FromPosTangentLateral(endLeftPos, endTangent, endLateral);
                    NewNodePosition = newNodePosition;
                    if (RoadTools.flattenTilt.Checked) newNodePosition.Tilt = 0;
                    if (RoadTools.flattenIncline.Checked) newNodePosition.Inclination = 0;

                } else {
                    //Take an existing end
                    var mouseOverNodeEnd = mouseOverLaneEnd.Value.RoadNodeEnd;
                    var end = LineEnd.calcLineEnd(mouseOverNodeEnd, mouseOverLane.MiddlePosition);
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
                var startDiff = startLateral * startWidth / 2;
                var endDiff = endLateral * endWidth / 2;
                Bezier3 lbound = GeometryUtils.GenerateJoinSpline(startPos - startDiff, endPos - endDiff, startTangent, -endTangent) + offset;
                Bezier3 rbound = GeometryUtils.GenerateJoinSpline(startPos + startDiff, endPos + endDiff, startTangent, -endTangent) + offset;
                Mesh renderBin = menu.renderHelper.GetOrCreateRenderBinForced(Assets.Road);
                RoadRenderer.DrawBezierStrip(lbound, rbound, renderBin, previewColor);
            }
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.AddSelectors(MultiMesh addTo, MultiMesh visibleSelectors) {
            SelectionUtils.AddAddLaneSelectors(menu);
        }

        public RoadTools RoadTools { get; private set; }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showFinishes);
            action.Add(ToolAttribs.showRoadTools);
        }
    }
}
