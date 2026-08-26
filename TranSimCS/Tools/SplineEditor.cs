using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.ClassInstances;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using SixLabors.ImageSharp.PixelFormats;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Tools {
    public class SplineEditor: ITool {


        public InGameMenu Menu { get; }
        public SplineEditor(InGameMenu menu) {
            this.Menu = menu; 
        }

        public RoadStrip? CurrentStrip { get; private set; }

        public string Name => "Edit road geometry";
        public string Description => (CurrentStrip == null) ? "Pick a road strip to edit it, or right-click it to reset." : "Edit the selected road strip. Draggable markers are yellow.";
        public (object[], string)[] PromptKeys() => (CurrentStrip != null) ? [
            ([MouseButton.Left], "to drag the selected handle"),
            ([MouseButton.Right], "on a handle to reset the selected handle"),
            ([MouseButton.Right], "anywhere else to quit editing"),
            ([Keys.Q], "to lock weight"),
            ([Keys.E], "to lock offset")
        ] : [
            ([MouseButton.Left], "to pick a road segment"),
            ([MouseButton.Right], "to reset geometry of a segment")
        ];

        void ITool.OnClick(MouseButton button) {
            var selectedRoadStrip = Menu.MouseOver?.As<RoadStrip>();
            switch (button) {
                case MouseButton.Left:
                    if(selectedRoadStrip != null) CurrentStrip = selectedRoadStrip;
                    break;
                case MouseButton.Right:
                    CurrentStrip = null;
                    break;
            }
        }

        public SplineDragState? DragState { get; private set; }
        public struct SplineDragState {
            public RoadStripHalf Handle;
            public Vector3 PreviousPosition;
            public SplineDragState(RoadStripHalf handle, Vector3 previousPosition) {
                Handle = handle;
                PreviousPosition = previousPosition;
            }
        }

        void ITool.Update(Microsoft.Xna.Framework.GameTime gameTime) {
            bool lmb = Menu.Game.MouseState.LeftButton == ButtonState.Pressed;
            var hoveringHandle = Menu.MouseOver?.As<RoadStripHalf>();

            if(lmb) {
                if(DragState == null) {
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

                var isWeightLocked = Menu.Game.KeyboardState.IsKeyDown(Keys.Q);
                var isOffsetLocked = Menu.Game.KeyboardState.IsKeyDown(Keys.E);

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
                    if(half == SegmentHalf.Start)
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
        void ITool.AddSelectors(MultiMesh invisibleSelectors, MultiMesh visibleSelectors) {
            //Draw selectors
            var renderBin = visibleSelectors.GetOrCreateRenderBinForced(Assets.White);
            void TickMark(Mesh renderBin, Vector3 pos, Vector3 normal, Vector3 tangent, Rgba32 c, float yoffset = 0.5f) {
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
