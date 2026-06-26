using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Tools {
    public class MoveTool(InGameMenu game) : ITool {
        string ITool.Name => "Move nodes and objects";

        string ITool.Description => "Drag and rotate objects";

        public Vector3 DragFrom { get; private set; }

        public MoveState? ObjToDrag { get; private set; }

        public struct MoveState {
            public IDraggableObj Object;
            public MoveNode[] Nodes;
            public Vector3 Pivot;
            public MoveState(IDraggableObj @object, MoveNode[]? nodes = null, Vector3? pivot = null) {
                ArgumentNullException.ThrowIfNull(@object, nameof(@object));
                this.Object = @object;
                this.Nodes = nodes ?? @object.DraggableComponents().Select(x => new MoveNode(x)).ToArray();
                this.Pivot = pivot ?? @object.FindCenter();
            }
            public void Apply(TransformQ transform) {
                for(int i = 0; i < Nodes.Length; i++) Nodes[i] = Nodes[i].Apply(transform, Pivot);
            }
        }
        public struct MoveNode {
            public IPosition Node;
            public TransformQ ObjPos;
            public MoveNode(IPosition node, TransformQ? objPos = null) {
                ArgumentNullException.ThrowIfNull(node, nameof(node));
                this.Node = node;
                this.ObjPos = objPos ?? Node.PositionData.ToTransformQ();
            }
            public MoveNode Apply(TransformQ transform, Vector3 pivot) {
                var result = this;
                result.ObjPos = ObjPos.Append(transform, pivot);
                Node.PositionData = ObjPos.ToObjPos();
                return result;
            }
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
                    var candidate = game.MouseOver?.SelectedObj;
                    if (candidate is IDraggableObj drag) ObjToDrag = new(drag);
                } else if (ObjToDrag != null) {
                    //Object is held
                    var obj = ObjToDrag.Value;
                    var dragFrom = DragFrom;
                    var delta = gs - dragFrom;
                    var mousedelta = game.Game.MouseState.Position - game.Game.MouseStateOld.Position;
                    var anglePerPx = MathF.PI / 360;
                    var angles = new Vector2(mousedelta.X, mousedelta.Y) * anglePerPx;
                    var cpos = obj.Pivot;

                    if (mousedelta == Point.Zero) return;

                    Quaternion q = Quaternion.Identity;
                    Vector3 offset = Vector3.Zero;

                    if (lmb & !rmb) {
                        //Drag
                        offset = delta;
                    }
                    if (rmb & !lmb) {
                        //Azimuth
                        q = Quaternion.CreateFromYawPitchRoll(angles.X, 0, 0);
                        offset = Vector3.UnitY * mousedelta.Y / -100;
                    }
                    if(lmb & rmb) {
                        //Tilt/inclination
                        var viewInv = Matrix.Invert(game.renderManager.Camera.GetViewMatrix());
                        var cameraRight = Vector3.Normalize(viewInv.Right);
                        var cameraUp = Vector3.Normalize(viewInv.Up);
                        var qHorizontal =Quaternion.CreateFromAxisAngle(cameraUp, angles.X);
                        var qVertical = Quaternion.CreateFromAxisAngle(cameraRight, angles.Y);
                        q = Quaternion.Normalize(qHorizontal * qVertical);
                    }
                    obj.Apply(new TransformQ(offset, q));
                }
                DragFrom = gs;
            } else {
                ObjToDrag = null;
            }
        }

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left, SpecialKey.MouseMove], "to move the object"),
            ([MouseButton.Right, SpecialKey.MouseMove], "to yaw/elevate the object"),
            ([MouseButton.Left, MouseButton.Right, SpecialKey.MouseMove], "to roll/pitch the object"),
        ];
    }
}
