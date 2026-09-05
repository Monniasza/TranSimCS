using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Tools;
using TranSimCS.Select;
using System.Numerics;
using TranSimCS.Worlds;
using Silk.NET.Input;

namespace TranSimCS.SilkNet.Mode {
    public class ModeSplit: IMode {
        public record struct BeforeMiddleAfter(float Before, float Middle, float After) {
            public void Deconstruct(out float before, out float middle, out float after) {
                before = Before;
                middle = Middle;
                after = After;
            }
        }

        public sealed class RoadT {
            public RoadStrip Road { get; }
            public float Extent { get; }
            public BeforeMiddleAfter ArcLengths { get; }
            public BeforeMiddleAfter Parameters { get; }

            public float SplitT => Parameters.Middle;

            public RoadT(RoadStrip road, float splitT, float extent) {
                var middlePos = road.ToolLUT.ByT[splitT].X;
                var length = road.ToolLUT.Length;
                var beforePos = middlePos - extent;
                if (beforePos < 0) beforePos = 0;
                var afterPos = middlePos + extent;
                if (afterPos > length) afterPos = length;
                var beforeT = road.ToolLUT.Forward[beforePos].W;
                var afterT = road.ToolLUT.Forward[afterPos].W;

                ArcLengths = new(beforePos, middlePos, afterPos);
                Parameters = new(beforeT, splitT, afterT);
                Road = road;
            }
        }

        public SilkNetTest Menu { get; }
        public RoadT? RoadPosition { get; private set; }
        public float SplitLength = 12;

        public ModeSplit(SilkNetTest menu) {
            Menu = menu;
        }

        

        string IMode.Title() => "Split road strips";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);
            if (ImGui.Begin("Split roads")) {
                var mouseover = Menu.MouseOver?.SelectedObj;
                if (mouseover is RoadStrip road)
                    ImGui.TextColored(green, "[LMB] to split here");
                else if (mouseover != null)
                    ImGui.TextColored(red, "The selected object is not splittable");
                else
                    ImGui.TextColored(maroon, "No object selected");
                ImGui.DragFloat("Split length (around the center) [m]", ref SplitLength, 0.05f, 1, 100);
                ImGui.End();
            }
        }
        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            float yoffset = 0.4f;
            float yoffset2 = 0.5f;

            void DrawTickMark(Mesh renderBin, OrthodistantBasis basis, float t, float hlength, float width, Color c) {
                var sample = basis.SampleFrame(t);
                var p0 = sample.O - sample.X * hlength + yoffset2 * sample.Y;
                var p1 = sample.O + sample.X * hlength + yoffset2 * sample.Y;
                renderBin.DrawLine(p0, p1, sample.Y, c, width);
            }

            if (RoadPosition != null) {
                var spline = RoadPosition.Road.ToolBasis;
                var renderBin = renderMeshPool.GetOrCreateRenderBinForced(Assets.WhiteTransparent);

                //Draw the spline
                SplitRoadMethods.DrawRoadSpline(RoadPosition.Road, renderBin, Colors.Cyan);

                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Before, 2, 1, Colors.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.After, 2, 1, Colors.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Middle, 1, 1, Colors.SkyBlue);
            }
        }
        void IMode.Update(double dt) {
            RoadPosition = null;

            var selection = Menu.MouseOver;
            var asRoadStrip = selection?.As<RoadStrip>();

            if (asRoadStrip == null) return;
            var point = selection.Value.Coordinates;

            var t = asRoadStrip.ToolBasis.UnTransform(point);
            RoadPosition = new(asRoadStrip, t.Z, SplitLength);
        }
        void IMode.OnMousePress(MouseButton button) {
            if (RoadPosition != null && button == MouseButton.Left) {
                //Split the road
                SplitRoad.SplitSegment(RoadPosition.Road, RoadPosition.Parameters.Before, RoadPosition.Parameters.After);
                Menu.MouseOver = null;
            }
        }
    }
}
