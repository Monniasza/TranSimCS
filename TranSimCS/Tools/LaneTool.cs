using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Tools {
    public class LaneTool: ITool {
        public string Name => "Lane Editor";

        public string Description => "Manipulate lanes precisely";

        public LaneTool(InGameMenu menu) {
            this.menu = menu;
            laneTools = menu.ToolsPanel.GetPanel<LaneTools>(ToolAttribs.showLaneManip);
            DeltaXProp = new(0, "deltax");
            DraggedLaneProp = new(null, "lane");
        }

        public readonly InGameMenu menu;
        public readonly LaneTools laneTools;
        public readonly Property<float> DeltaXProp;
        public readonly Property<Lane?> DraggedLaneProp;


        public float DeltaX {
            get => DeltaXProp.Value;
            set => DeltaXProp.Value = value;
        }
        public Lane DraggedLane {
            get => DraggedLaneProp.Value;
            set => DraggedLaneProp.Value = value;
        }


        public void Draw(GameTime gameTime) {
            //draw guide arrows over every lane
            foreach(var node in menu.World.Nodes.data) {
                var refframe = node.ReferenceFrame;
                foreach (var lane in node.Lanes) {
                    var bounds = lane.Bounds;
                    var arrowBin = menu.renderHelper.GetOrCreateRenderBinForced(Assets.Arrow);
                    RoadRenderer.GenerateLaneMesh(lane, arrowBin, 0.5f);
                }
            }
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            return [
                (["drag", MouseButton.Left], "Move the left border"),
                (["drag", MouseButton.Right], "Move the right border"),
                (["click", MouseButton.Middle], "Select for precision")
            ];
        }

        public void Update(GameTime gameTime) {
            var lmbOld = menu.Game.MouseStateOld.LeftButton == ButtonState.Pressed;
            var rmbOld = menu.Game.MouseStateOld.RightButton == ButtonState.Pressed;
            var lmbNew = menu.Game.MouseState.LeftButton == ButtonState.Pressed;
            var rmbNew = menu.Game.MouseState.RightButton == ButtonState.Pressed;

            var pressedBefore = lmbOld || rmbOld;
            var pressedNow = lmbNew || rmbNew;

            if(pressedNow & !pressedBefore) {
                DeltaX = 0;
                DraggedLane = menu.MouseOverRoad?.SelectedLane;
                if (laneTools.SnappingIsAbsolute && DraggedLane != null) {
                    var bounds = DraggedLane.Bounds;
                    var newLeft = bounds.Min;
                    var newRight = bounds.Max;
                    var snapIncrement = laneTools.SnappingResult;
                    if (snapIncrement != 0) {
                        newLeft = snapIncrement * MathF.Round(newLeft / snapIncrement);
                        newRight = snapIncrement * MathF.Round(newRight / snapIncrement);
                    }
                    var difference = (newLeft + newRight - bounds.Min - bounds.Max) / 2;

                    if(newLeft <= newRight) {
                        DraggedLane.Bounds = new(newLeft, newRight);
                        DeltaX += difference;
                    }
                }
            }

            var selectedLane = DraggedLane;
            if(pressedNow & pressedBefore && selectedLane != null) {
                var prevMouse = menu.MouseRayOld;
                var nextMouse = menu.MouseRay;
                var refframe = selectedLane.RoadNode.ReferenceFrame;
                var plane = refframe.XZPlane();

                var oldPos = GeometryUtils.IntersectRayPlane(prevMouse, plane);
                var newPos = GeometryUtils.IntersectRayPlane(nextMouse, plane);
                var oldX = Vector3.Dot(oldPos - refframe.O, refframe.X);
                var newX = Vector3.Dot(newPos - refframe.O, refframe.X);
                DeltaX += newX - oldX;

                //Snap the values
                //Handle relative snapping
                var dx = DeltaX;
                var snapIncrement = laneTools.SnappingResult;
                if (snapIncrement != 0) {
                    var sgn = MathF.Sign(dx);
                    dx = MathF.Abs(dx);
                    dx = sgn * snapIncrement * MathF.Floor(dx / snapIncrement);
                }

                if (dx != 0) {
                    var bounds = selectedLane.Bounds;
                    var newLeft = bounds.Min;
                    var newRight = bounds.Max;
                    if (lmbNew) {
                        newLeft += dx;
                        if (!rmbNew && newLeft > newRight) newLeft = newRight;
                    }
                    if (rmbNew) {
                        newRight += dx;
                        if (!lmbNew && newRight < newLeft) newRight = newLeft;
                    }
                    selectedLane.Bounds = new MonoGame.Extended.Range<float>(newLeft, newRight);
                    DeltaX -= dx;
                }
            }

            if(pressedBefore & !pressedNow) {
                DraggedLane = null;
            }
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showLaneManip);
        }
    }
}
