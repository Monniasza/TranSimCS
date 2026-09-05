using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet.Mode {
    public class ModeMoveIt(SilkNetTest game) : IMode {
        string IMode.Title() => "Move It!";

        public Vector3 DragFrom { get; private set; }

        public MoveState? ObjToDrag { get; private set; }

        void IMode.DrawUI() {
            if(ImGui.Begin("Move It!")) {
                var lmbOld = game.MouseStateOld.IsMouseButtonDown(Silk.NET.Input.MouseButton.Left);
                var rmbOld = game.MouseStateOld.IsMouseButtonDown(Silk.NET.Input.MouseButton.Right);
                ImGui.Text($"Old mouse button states: [{lmbOld}] [{rmbOld}]");
                ImGui.Text("Hold [LMB] to drag on X/Y");
                ImGui.Text("Hold [RMB] to yaw/elevate the object");
                ImGui.Text("Hold [LMB+RMB] to roll/pitch the object");
                ImGui.End();
            }
        }

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
                for (int i = 0; i < Nodes.Length; i++) Nodes[i] = Nodes[i].Apply(transform, Pivot);
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

        void IMode.Update(double dt) {
            var lmb    = game.MouseState   .IsMouseButtonDown(Silk.NET.Input.MouseButton.Left);
            var rmb    = game.MouseState   .IsMouseButtonDown(Silk.NET.Input.MouseButton.Right);
            var lmbOld = game.MouseStateOld.IsMouseButtonDown(Silk.NET.Input.MouseButton.Left);
            var rmbOld = game.MouseStateOld.IsMouseButtonDown(Silk.NET.Input.MouseButton.Right);
            var plane = new Plane(0, 1, 0, 0);
            var gs = GeometryUtils.IntersectRayPlane(game.MouseRay, plane);
            if (lmb | rmb) {
                if ((!lmbOld & lmb) | (!rmbOld & rmb)) {
                    //Object newly clicked
                    var candidate = game.MouseOver?.SelectedObj;
                    if (candidate is IDraggableObj drag) ObjToDrag = new(drag);
                } else if (ObjToDrag != null) {
                    //Object is held
                    var obj = ObjToDrag.Value;
                    var dragFrom = DragFrom;
                    var delta = gs - dragFrom;
                    var mousedelta = game.MousePosition - game.MousePositionPrev;
                    var anglePerPx = MathF.PI / 360;
                    var angles = new Vector2(mousedelta.X, mousedelta.Y) * anglePerPx;

                    if (mousedelta.X == 0 && mousedelta.Y == 0) return;

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
                    if (lmb & rmb) {
                        //Tilt/inclination
                        Matrix4x4.Invert(game.RenderManager.WorldViewProjection, out var viewInv);
                        var cameraRight = Vector3.Normalize(new(viewInv.M11, viewInv.M12, viewInv.M13));
                        var cameraUp = Vector3.Normalize(new(viewInv.M31, viewInv.M32, viewInv.M33));
                        var qHorizontal = Quaternion.CreateFromAxisAngle(cameraUp, angles.X);
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

        
    }
}
