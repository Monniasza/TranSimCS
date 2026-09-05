using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
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
                var middlePos = road.ToolLUT.ByT[splitT].X;
                var length = road.ToolLUT.Length;
                var beforePos = middlePos - extent;
                if(beforePos < 0) beforePos = 0;
                var afterPos = middlePos + extent;
                if(afterPos > length) afterPos = length;
                var beforeT = road.ToolLUT.Forward[beforePos].W;
                var afterT = road.ToolLUT.Forward[afterPos].W;

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
                var spline = RoadPosition.Road.ToolBasis;
                var renderBin = Menu.renderHelper.GetOrCreateRenderBinForced(Assets.WhiteTransparent);

                //Draw the spline
                SplitRoadMethods.DrawRoadSpline(RoadPosition.Road, renderBin, Colors.Cyan);

                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Before, 2, 1, Colors.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.After, 2, 1, Colors.DeepSkyBlue);
                DrawTickMark(renderBin, spline, RoadPosition.Parameters.Middle, 1, 1, Colors.SkyBlue);
            }
        }
        void ITool.Update(GameTime gameTime) {
            RoadPosition = null;

            var selection = Menu.MouseOver;
            var asRoadStrip = selection?.As<RoadStrip>();

            if (asRoadStrip == null) return;
            var point = selection.Value.Coordinates;

            var t = asRoadStrip.ToolBasis.UnTransform(point);
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
