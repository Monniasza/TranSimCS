using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus;
using TranSimCS.Menus.Gizmo;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
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
            var renderBin = visibleSelectors.GetOrCreateRenderBinForced(Assets.Add);

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
}
