using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.Gizmo;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class MoveTool(InGameMenu game) : ITool {
        string ITool.Name => "Move nodes and objects";

        string ITool.Description => "Drag and rotate objects";

        public Vector3 DragFrom { get; private set; }

        public IDraggableObj? ObjToDrag { get; private set; }

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
            var lmb = game.Game.MouseState.LeftButton == ButtonState.Pressed;
            var rmb = game.Game.MouseState.RightButton == ButtonState.Pressed;
            var lmbOld = game.Game.MouseStateOld.LeftButton == ButtonState.Released;
            var rmbOld = game.Game.MouseStateOld.RightButton == ButtonState.Released;


            var gs = game.GroundSelection;
            if (lmb | rmb) {
                if((lmbOld & lmb) | (rmbOld & rmb)) {
                    //Object newly clicked
                    //ObjToDrag = game.MouseOverRoad?.SelectedRoadNode;
                    var candidate = game.SelectedObject;
                    if (candidate is IDraggableObj drag) ObjToDrag = drag;
                } else if (ObjToDrag != null) {
                    //Object is held
                    var dragFrom = DragFrom;
                    var delta = gs - dragFrom;
                    var mousedelta = game.Game.MouseState.Position - game.Game.MouseStateOld.Position;
                    var anglePerPx = MathF.PI / 360;
                    var angles = new Vector2(anglePerPx * mousedelta.X, anglePerPx * mousedelta.Y);

                    if (lmb & !rmb) {
                        //Drag
                        ObjToDrag?.Drag(delta, dragFrom);
                    }
                    if (rmb & !lmb) {
                        //Azimuth
                        ObjToDrag?.Rotate(GeometryUtils.RadiansToField(angles.X), 0, 0);
                    }
                    if(lmb & rmb) {
                        //Tilt/inclination
                        ObjToDrag?.Rotate(0, angles.Y, angles.X);
                    }
                }
                DragFrom = gs;
            } else {
                ObjToDrag = null;
            }
        }

        void ITool.AddSelectors(MultiMesh addTo, MultiMesh visibleSelectors) {
            //Add azimuth gizmos
            var renderBin = visibleSelectors.GetOrCreateRenderBinForced(Assets.Add);

            /*foreach (var roadNode in game.World.RoadNodes) {
                var azimuthGizmo = new AzimuthGizmo(roadNode);
                azimuthGizmo.CreateMesh(renderBin);
            }*/

        }

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left, SpecialKey.MouseMove], "to move the object"),
            ([MouseButton.Right, SpecialKey.MouseMove], "to yaw the object"),
            ([MouseButton.Left, MouseButton.Right, SpecialKey.MouseMove], "to roll/pitch the object"),
        ];
    }
}
