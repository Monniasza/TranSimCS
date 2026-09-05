using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet.Mode {
    public class ModeConnection : IMode {
        //Tool definition
        public readonly SilkNetTest menu;

        //Tool state
        public HalfLane? SourceNode { get; private set; }
        public HalfLane? DestNode { get; private set; }
        public LaneStrip? LaneStrip { get; private set; }

        //Tool properties
        public string Title() => "Road Connection Editor";
        void IMode.DrawUI() {
            if (_description == null) return;
            if(ImGui.Begin("Road Connection Editor")) {
                ImGui.Text(_description);
                menu.ShowLaneCreator();
                menu.ShowFinishSettings();
                ImGui.End();
            }
        }

        //Cached state
        private NextAction nextAction;
        private Color actionColor;
        private string _description;
        private enum NextAction {
            Pick, Hover, Add, Reverse, Edit, Delete
        }
        private static (string description, Color color) GetForAction(NextAction nextAction) => nextAction switch {
            NextAction.Pick => ("[LMB] over a lane end to start editing connections", Colors.Transparent),
            NextAction.Hover => ("Editing connections. [LMB] over lane ends to add, remove and modify connections. [RMB] to cancel", Colors.White),
            NextAction.Add => ("Editing connections. [LMB] to add a connection. [LAlt] for reverse. [RMB] to cancel", Colors.Green),
            NextAction.Reverse => ("Editing connections. [LMB] to reverse a connection. [RMB] to cancel", Colors.Cyan),
            NextAction.Edit => ("Editing connections. [LMB] to modify a connection. [LAlt] to reverse instead. [RMB] to cancel", Colors.Yellow),
            NextAction.Delete => ("Editing connections. [LMB] to delete a connection. [LAlt] to reverse instead. [RMB] to cancel", Colors.Red),
            _ => throw new ArgumentException("Invalid NextAction: " + nextAction)
        };

        public ModeConnection(SilkNetTest menu) {
            this.menu = menu;
        }

        void IMode.Update(double dt) {
            if (SourceNode != null) DestNode = menu.MouseOver?.As<HalfLane>();
            if (SourceNode == DestNode) DestNode = null;

            if (SourceNode == null || DestNode == null) {
                LaneStrip = null;
            } else {
                LaneStrip = menu.World.FindLaneStrip(SourceNode, DestNode);
            }

            if (SourceNode == null) nextAction = NextAction.Pick;
            else if (DestNode == null) nextAction = NextAction.Hover;
            else if (LaneStrip == null) nextAction = NextAction.Add;
            else if (ImGui.IsKeyDown(ImGuiKey.LeftAlt)) nextAction = NextAction.Reverse;
            else if (!LaneStrip.LaneSpec.EqualsExceptWidth(menu.LaneSpec)) nextAction = NextAction.Edit;
            else nextAction = NextAction.Delete;

            (_description, actionColor) = GetForAction(nextAction);
        }

        void IMode.OnMousePress(MouseButton button) {
            if (button == MouseButton.Right) {
                SourceNode = DestNode = null;
                LaneStrip = null;
                return;
            }

            var pickedLaneEnd = menu.MouseOver?.As<HalfLane>();
            if (button == MouseButton.Left) switch (nextAction) {
                case NextAction.Pick:
                    SourceNode = pickedLaneEnd;
                    break;
                case NextAction.Edit:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Edit");
                    LaneStrip.LaneSpec = menu.LaneSpec;
                    break;
                case NextAction.Delete:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Delete");
                    LaneStrip.Destroy();
                    menu.MouseOver = null;
                    break;
                case NextAction.Add:
                    Debug.Assert(LaneStrip == null, "Got Add with an already existing lane strip");
                    Debug.Assert(DestNode != null, "Invalid destination for AddNode");
                    Debug.Assert(SourceNode != null, "Invalid source for AddNode");
                    var sourceLane = SourceNode;
                    var destLane = DestNode;
                    if (ImGui.IsKeyDown(ImGuiKey.LeftAlt))
                        DataUtil.Swap(ref sourceLane, ref destLane);
                    menu.World.GetOrMakeLaneStrip(sourceLane, destLane, menu.RoadFinish, menu.LaneSpec);
                    break;
                case NextAction.Reverse:
                    Debug.Assert(LaneStrip != null, "Invalid lane strip for Reverse");
                    LaneStrip.ReverseDirection();
                    menu.MouseOver = null;
                    break;
            }
        }

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            if (SourceNode == null) return;

            var yoffset = 0.1f;

            var sourceLane = SourceNode;
            var sourceFrame = sourceLane.HalfNode.Cache.ReferenceFrame;
            var centerStartIndex = sourceLane.MiddlePosition;
            var startPos = sourceFrame.O + sourceFrame.X * centerStartIndex + sourceFrame.Y * yoffset;

            var renderBin = renderMeshPool.GetOrCreateRenderBinForced(Assets.WhiteTransparent);
            var color = actionColor;
            float width = 0.5f;

            if (DestNode == null) {
                Plane plane = new Plane(0, 1, 0, 0);
                var endPos = GeometryUtils.IntersectRayPlane(menu.MouseRay, plane);
                var dist = Vector3.DistanceSquared(startPos, endPos);
                if (dist > 0.0001) renderBin.DrawLine(startPos, endPos, sourceFrame.Y, color, width);
            } else {
                var endLane = DestNode;
                var endFrame = endLane.HalfNode.Cache.ReferenceFrame;
                var centerEndIndex = endLane.MiddlePosition;
                var endPos = endFrame.O + endFrame.X * centerEndIndex + sourceFrame.Y * yoffset;

                var dist = Vector3.DistanceSquared(startPos, endPos);
                if (dist < 0.0001f) return;

                var spline = GeometryUtils.GenerateJoinSpline(startPos, endPos, sourceFrame.Z, endFrame.Z);
                var points = GeometryUtils.GenerateSplinePoints(spline, Settings.RoadAccuracy);
                for (int i = 1; i < points.Length; i++) {
                    var a = points[i];
                    var b = points[i - 1];
                    renderBin.DrawLine(a, b, sourceFrame.Y, color, width);
                }
            }
        }
    }
}
