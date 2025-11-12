using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Spline;
using TranSimCS.Tools;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu {
        private void DrawHighlights(GameTime time) {
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Road);

            //If a road segment is selected, draw the selection
            var roadSelection = MouseOverRoad;
            if (roadSelection?.SelectedLaneTag != null) {
                // Draw the selected lane tag with a different color
                var laneRange = roadSelection.SelectedLaneTag.Value;
                RoadRenderer.GenerateLaneRangeMesh(laneRange, renderBin, laneHighlightColor, 0.5f);
                var fstag = laneRange.road.FullSizeTag();
                if (fstag != null) RoadRenderer.GenerateLaneRangeMesh(fstag.Value, renderBin, roadSegmentHighlightColor, 0.45f);
                var splines = RoadRenderer.GenerateSplines(laneRange, 0.55f);
                Bezier3.TriSection(splines.Item1, minT, maxT, out Bezier3 leftSubBezier1, out Bezier3 leftSubBezier2, out Bezier3 leftSubBezier3);
                Bezier3.TriSection(splines.Item2, minT, maxT, out Bezier3 rightSubBezier1, out Bezier3 rightSubBezier2, out Bezier3 rightSubBezier3);

                // Draw the left and right bezier curves of the selected lane tag
                if (roadSelection.SelectedLaneT < minT) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier1, rightSubBezier1, renderBin, laneHighlightColor2);
                } else if (roadSelection.SelectedLaneT < maxT) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier2, rightSubBezier2, renderBin, laneHighlightColor2);
                } else {
                    RoadRenderer.DrawBezierStrip(leftSubBezier3, rightSubBezier3, renderBin, laneHighlightColor2);
                }
            }

            //Draw the selected road node
            if (roadSelection?.SelectedLaneEnd != null && roadSelection.SelectedLaneStrip == null) {
                //Lane selected, road strip not
                var lane = roadSelection.SelectedLaneEnd.Value;
                var quad = RoadRenderer.GenerateLaneQuad(lane, 0.5f, Color.Yellow);
                var nodeQuad = RoadRenderer.GenerateRoadNodeSelQuad(lane.lane.RoadNode, roadSegmentHighlightColor, 0.45f);
                renderBin.DrawQuad(quad);
                renderBin.DrawQuad(nodeQuad);
            }
        }


        public override void Draw(GameTime time) {
            //Clear the screen to a solid color and clear the render helper
            renderHelper.Clear();

            // Draw the asphalt texture for the road
            foreach (var roadSegment in World.RoadSegments.data) renderHelper.AddAll(roadSegment.Mesh.GetMesh());

            //Draw road sections
            foreach (var section in World.RoadSections.data) renderHelper.AddAll(section.Mesh.GetMesh());

            //Draw buildings
            foreach (var building in World.Buildings.data) renderHelper.AddAll(building.Mesh.GetMesh());

            //Draw cars
            foreach (var car in World.Cars.data) renderHelper.AddAll(car.Mesh.GetMesh());

            bool suppressHighlights = ToolAttributes.Contains(ToolAttribs.noHighlights);
            if (!suppressHighlights) DrawHighlights(time);

            //Draw node selectors
            if (CheckNodes.Checked)
                foreach (var node in World.Nodes.data)
                    renderHelper.AddAll(node.Mesh.GetMesh());

            //Draw SelectorObjects
            renderHelper.AddAll(SelectorObjects);

            

            //If the add lane button is selected, draw it
            IRenderBin plusRenderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Add);
            if (SelectedObject is AddLaneSelection selection)
                RoadRenderer.CreateAddLane(selection, plusRenderBin, configuration.LaneSpec.Width, roadSegmentHighlightColor, 0.5f);

            //Render ground with multiple planes
            var centerPos = renderManager.Camera.Position;
            IRenderBin grassBin = renderHelper.GetOrCreateRenderBinForced(Assets.Grass);
            RenderGround(centerPos, grassBin);

            //Render road tool
            configuration.Tool?.Draw(time);

            //Render the render helper
            var tris = 0;
            var verts = 0;
            foreach (var bin in renderHelper.RenderBins.Values) {
                tris += (bin.Indices.Count) / 3;
                verts += bin.Vertices.Count;
            }
            renderManager.Render(renderHelper);
        }

        private void RenderGround(Vector3 posoffset, IRenderBin renderBin) {
            posoffset.Y = 0;

            //Render the center
            GroundParallelogram(renderBin, posoffset, new(-1, 0, -1), Vector3.UnitX * 2, Vector3.UnitZ * 2, 1000);

            //Render concentric rings, each 2 times bigger
            float scale = 1000;
            Vector3[] basisVectors = new Vector3[] {
                Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitX
            };

            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < 4; j++) {
                    var prevVector = basisVectors[j];
                    var nextVector = basisVectors[j + 1];
                    GroundParallelogram(renderBin, posoffset, -(nextVector + (prevVector * 2)), prevVector, nextVector * 3, scale);
                }
                scale *= 2;
            }
        }
        private void GroundParallelogram(IRenderBin renderBin, Vector3 initialpos, Vector3 basepos, Vector3 xplus, Vector3 yplus, float scale) {
            var a = (initialpos + basepos * scale);
            var s = scale / 100;
            var C = Color.White;
            var xmul = xplus * scale;
            var ymul = yplus * scale;
            var b = a + ymul;
            var c = b + xmul;
            var d = a + xmul;
            renderBin.DrawQuad(
                GenerateGroundVertex(a, s, C),
                GenerateGroundVertex(b, s, C),
                GenerateGroundVertex(c, s, C),
                GenerateGroundVertex(d, s, C)
            );
        }
        private VertexPositionColorTexture GenerateGroundVertex(Vector3 pos, float texscale, Color? color = null) {
            var c = color ?? Color.White;
            return new VertexPositionColorTexture(pos, c, new(pos.X / texscale, pos.Z / texscale));
        }
    }
}
