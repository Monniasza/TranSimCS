using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.Tools;

namespace TranSimCS.SilkNet.Mode {
    public class ModeSpline(SilkNetTest menu) : IMode {
        public SilkNetTest Menu { get; } = menu;
        public RoadStrip? CurrentStrip { get; private set; }
        public SplineDragState? DragState { get; private set; }
        public struct SplineDragState {
            public RoadStripHalf Handle;
            public Vector3 PreviousPosition;
            public SplineDragState(RoadStripHalf handle, Vector3 previousPosition) {
                Handle = handle;
                PreviousPosition = previousPosition;
            }
        }

        string IMode.Title() => "Spline Editor";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            var selectedRoadStrip = Menu.MouseOver?.SelectedObj;
            var selectedTag = Menu.MouseOver?.Tag;
            if (ImGui.Begin("Spline Editor")) {
                if(CurrentStrip != null) {
                    if (selectedTag is RoadStripHalf) {
                        ImGui.TextColored(green, "[LMB] Drag yellow handles to adjust geometry");
                        ImGui.TextColored(green, "[RMB] to reset the selected handle offset");
                    } else {
                        if (selectedTag != null) {
                            ImGui.TextColored(red, "The selected object is not a handle");
                            ImGui.TextColored(red, "[RMB] to quit editing geometry");
                        } else {
                            ImGui.TextColored(maroon, "No object selected");
                            ImGui.TextColored(maroon, "[RMB] to quit editing geometry");
                        }
                            
                    }
                    ImGui.Text("[Q] to lock weight");
                    ImGui.Text("[E] to lock offset");
                } else {
                    if (selectedRoadStrip is RoadStrip road) {
                        ImGui.TextColored(green, "[LMB] to start editing geometry");
                        ImGui.TextColored(green, "[RMB] to reset geometry");
                    } else if (selectedRoadStrip != null)
                        ImGui.TextColored(red, "The selected object is not a road strip");
                    else
                        ImGui.TextColored(maroon, "No object selected");
                }
                ImGui.End();
            }
        }
        void IMode.OnMousePress(MouseButton button) {
            var selectedRoadStrip = Menu.MouseOver?.As<RoadStrip>();
            switch (button) {
                case MouseButton.Left:
                    if (selectedRoadStrip != null) CurrentStrip = selectedRoadStrip;
                    break;
                case MouseButton.Right:
                    if (CurrentStrip != null && Menu.MouseOver?.Tag is RoadStripHalf sh && sh.RoadStrip != null) {
                        if (sh.SegmentHalf == SegmentHalf.Start) {
                            sh.RoadStrip.OverrideSplineStartPos = null;
                        } else {
                            sh.RoadStrip.OverrideSplineEndPos = null;
                        }
                    } else if (selectedRoadStrip != null && CurrentStrip == null) {
                        selectedRoadStrip.OverrideSplineEndPos = null;
                        selectedRoadStrip.OverrideSplineStartPos = null;
                        selectedRoadStrip.SplineWeightStart = 1;
                        selectedRoadStrip.SplineWeightEnd = 1;
                    } else {
                        CurrentStrip = null;
                    }
                    break;
            }
        }

        void IMode.Update(double dt) {
            bool lmb = Menu.MouseState.IsMouseButtonDown(MouseButton.Left);
            var hoveringHandle = Menu.MouseOver?.As<RoadStripHalf>();

            if (lmb) {
                if (DragState == null) {
                    if (hoveringHandle == null || hoveringHandle?.RoadStrip == null) return;
                    SplineDragState dragState = new SplineDragState();
                    dragState.Handle = hoveringHandle.Value;
                    dragState.PreviousPosition = Menu.MouseOver!.Value.Coordinates;
                    DragState = dragState;
                }

                //Moved
                Debug.Assert(DragState != null, "Mouse buttons held, but no drag state");

                var handle = DragState.Value.Handle;
                var road = handle.RoadStrip;
                var half = handle.SegmentHalf;
                var oldPos = DragState.Value.PreviousPosition;

                Debug.Assert(road != null, "Nonnull drag state with null road");

                var isWeightLocked = ImGui.IsKeyDown(ImGuiKey.Q);
                var isOffsetLocked = ImGui.IsKeyDown(ImGuiKey.E);

                var splineReferenceFrame = half.GetConditional(road.StartNode.Cache.ReferenceFrame, road.EndNode.Cache.ReferenceFrame);
                var plane = splineReferenceFrame.XZPlane();

                var newPosition = GeometryUtils.IntersectRayPlane(Menu.MouseRay, plane);
                var dPos = newPosition - oldPos;


                var dOffset = Vector3.Dot(splineReferenceFrame.X, dPos);
                var dLength = Vector3.Dot(splineReferenceFrame.Z, dPos);
                if (!isOffsetLocked) {
                    var offsets = road.GetSplinePositions();
                    if (half == SegmentHalf.Start)
                        road.OverrideSplineStartPos = offsets.X + dOffset;
                    else
                        road.OverrideSplineEndPos = offsets.Y + dOffset;
                }
                if (!isWeightLocked) {
                    var lengths = road.GetSplineLengths();
                    if (half == SegmentHalf.Start)
                        lengths.X += dLength;
                    else
                        lengths.Y += dLength;
                    if (lengths.X < 0.1) lengths.X = 0.1f;
                    if (lengths.Y < 0.1) lengths.Y = 0.1f;
                    road.SetTangentLengths(lengths);
                }

                DragState = new SplineDragState(handle, newPosition);
            } else {
                DragState = null;
            }
        }
        void IMode.AddSelectors(MultiMesh invisible, MultiMesh visible) {
            //Draw selectors
            var renderBin = visible.GetOrCreateRenderBinForced(Assets.White);
            void TickMark(Mesh renderBin, Vector3 pos, Vector3 normal, Vector3 tangent, Color c, float yoffset = 0.5f) {
                pos += normal * yoffset;
                var pos1 = pos - tangent * 0.5f;
                var pos2 = pos + tangent * 0.5f;
                renderBin.DrawLine(pos1, pos2, normal, c, 1);
            }

            if (CurrentStrip != null) {
                SplitRoadMethods.DrawRoadSpline(CurrentStrip, renderBin, Colors.Magenta);
                var centerspline = CurrentStrip.ToolBasis.ReferenceSpline;
                var normalspline = CurrentStrip.ToolBasis.NormalSpline;

                var startTangent = Vector3.Normalize(centerspline.b - centerspline.a);
                var endTangent = Vector3.Normalize(centerspline.c - centerspline.d);
                var startNormal = Vector3.Normalize(normalspline.a);
                var endNormal = Vector3.Normalize(normalspline.d);

                //Draw endpoints
                TickMark(renderBin, centerspline.a, startNormal, startTangent, Colors.Cyan);
                TickMark(renderBin, centerspline.d, endNormal, endTangent, Colors.Cyan);

                TickMark(renderBin, centerspline.b, startNormal, startTangent, Colors.Yellow);
                renderBin.AddTagsToLastTriangles(2, new RoadStripHalf(CurrentStrip, SegmentHalf.Start));
                TickMark(renderBin, centerspline.c, endNormal, endTangent, Colors.Yellow);
                renderBin.AddTagsToLastTriangles(2, new RoadStripHalf(CurrentStrip, SegmentHalf.End));

                var offset1 = startNormal * 0.45f;
                var offset2 = endNormal * 0.45f;
                renderBin.DrawLine(centerspline.a + offset1, centerspline.b + offset1, startNormal, Colors.White);
                renderBin.DrawLine(centerspline.d + offset2, centerspline.c + offset2, endNormal, Colors.White);
            }
        }
    }
}
