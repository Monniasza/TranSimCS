using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Spline;

namespace TranSimCS.Tools {
    public sealed class SplitRoadTool : ITool {
        public record struct BeforeMiddleAfter (float Before, float Middle, float After) {
            public void Deconstruct(out float before, out float middle, out float after) {
                before = Before;
                middle = Middle;
                after = After;
            }
        }

        public sealed class RoadT {
            public RoadStrip Road { get; }
            public float Extent {  get; }
            public BeforeMiddleAfter ArcLengths { get; }
            public BeforeMiddleAfter Parameters { get; }

            public float SplitT => Parameters.Middle;
            
            public RoadT(RoadStrip road, float splitT, float extent) {
                var middlePos = road.InterCenterLUT.ByT[splitT].X;
                var length = road.InterCenterLUT.Length;
                var beforePos = middlePos - extent;
                if(beforePos < 0) beforePos = 0;
                var afterPos = middlePos + extent;
                if(afterPos > length) afterPos = length;
                var beforeT = road.InterCenterLUT.Forward[beforePos].W;
                var afterT = road.InterCenterLUT.Forward[afterPos].W;

                ArcLengths = new(beforePos, middlePos, afterPos);
                Parameters = new(beforeT, splitT, afterT);
                Road = road;
            }
        }

        public InGameMenu Menu { get; }
        public SplitRoadTab SplitOptions { get; }
        public SplitRoadTool(InGameMenu menu) {
            Menu = menu;
            SplitOptions = menu.ToolsPanel.GetPanel<SplitRoadTab>(ToolAttribs.showRoadSplit);
        }

        public string Name => "Split road strips";

        public string Description => "";

        public RoadT? RoadPosition {get; private set;}

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "to split a road")
        ];

        void ITool.Draw(GameTime gameTime) {
            float yoffset = 0.4f;
            float yoffset2 = 0.5f;

            void DrawTickMark(Mesh renderBin, OrthodistantBasis basis, float t, float hlength, float width, Color c) {
                var sample = basis.SampleFrame(t);
                var p0 = sample.O - sample.X * hlength + yoffset2 * sample.Y;
                var p1 = sample.O + sample.X * hlength + yoffset2 * sample.Y;
                renderBin.DrawLine(p0, p1, sample.Y, c, width);
            }

            if(RoadPosition != null) {
                var spline = RoadPosition.Road.InterCenterBasis;
                var renderBin = Menu.renderHelper.GetOrCreateRenderBinForced(Assets.WhiteTransparent);

                //Draw the spline
                var accuracy = Settings.RoadAccuracy;
                var step = 1.0f / (accuracy - 1);
                var points = new Transform3[accuracy];
                for (int i = 0; i < accuracy; i++) {
                    points[i] = spline.SampleFrame(i * step);
                }
                    
                for(int i = 1; i < accuracy; i++) {
                    var prev = points[i - 1];
                    var next = points[i];
                    var c0 = prev.O + yoffset * prev.Y;
                    var c1 = next.O + yoffset * prev.Y;
                    var normal = Vector3.Normalize(prev.Y + next.Y);

                    renderBin.DrawLine(c0, c1, normal, Color.Cyan, 1);
                }

                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Before, 2, 1, Color.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.After, 2, 1, Color.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Middle, 1, 1, Color.SkyBlue);
            }
        }
        void ITool.Update(GameTime gameTime) {
            RoadPosition = null;

            var selection = Menu.MouseOver;
            var asRoadStrip = selection?.As<RoadStrip>();

            if (asRoadStrip == null) return;
            var point = selection.Value.Coordinates;

            var t = asRoadStrip.InterCenterBasis.UnTransform(point);
            RoadPosition = new(asRoadStrip, t.Z, SplitOptions.SplitLength);
        }
        void ITool.OnClick(MouseButton button) {
            if(RoadPosition != null && button == MouseButton.Left) {
                //Split the road
                SplitRoad.SplitSegment(RoadPosition.Road, RoadPosition.Parameters.Before, RoadPosition.Parameters.After);
                Menu.MouseOver = null;
            }
        }
        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showRoadSplit);
        }
    }
}
