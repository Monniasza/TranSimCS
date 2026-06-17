using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Marking;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu {
        public Stats Stats { get; private set; }

        private void DrawHighlights(GameTime time) {
            Mesh renderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Road);

            var roadStrip = MouseOver?.GetRoadStrip();
            if((MouseOver?.SelectedObj is RoadStrip strip)) {
                var fstag = strip.FullSizeTag();
                RoadRenderer.GenerateLaneRangeMesh(fstag, renderBin, roadSegmentHighlightColor, 0.45f);
            }

            //If a road segment is selected, draw the selection
            if ((MouseOver?.Tag) is LaneStrip laneStrip) {
                // Draw the selected lane tag with a different color
                var laneTag = laneStrip.Tag();
                RoadRenderer.GenerateLaneRangeMesh(laneTag, renderBin, laneHighlightColor, 0.5f);
            }

            //Draw the selected road node
            var selectedObj = MouseOver?.Tag;
            if (selectedObj is IRoadElement element && element.GetLaneEnd() != null && element.GetRoadStrip() == null) {
                //Lane selected, road strip not
                var lane = element.GetLaneEnd();
                var quad = RoadRenderer.GenerateLaneQuad(lane.Value, 0.5f, Color.Yellow);
                var nodeQuad = RoadRenderer.GenerateRoadNodeSelQuad(element.GetRoadNode(), roadSegmentHighlightColor, 0.45f);
                renderBin.DrawQuad(quad);
                renderBin.DrawQuad(nodeQuad);
            }
        }


        public override void Draw(GameTime time) {
            //Clear stats
            Stats stats = default;

            //Clear the screen to a solid color and clear the render helper
            renderHelper.Clear();

            // Draw the asphalt texture for the road
            stats.Segments = World.RoadSegments.data.Count;
            foreach (var roadSegment in World.RoadSegments.data) {
                stats.Strips += roadSegment.Lanes.Count;
                renderHelper.AddAll(roadSegment.Mesh.GetMesh());
            }

            //Draw road sections
            stats.Sections = World.RoadSections.data.Count;
            foreach (var section in World.RoadSections.data) renderHelper.AddAll(section.Mesh.GetMesh());

            //Draw buildings
            stats.Buildings = World.Buildings.data.Count;
            foreach (var building in World.Buildings.data) renderHelper.AddAll(building.Mesh.GetMesh());

            //Draw cars
            stats.Cars = World.Cars.data.Count;
            foreach (var car in World.Cars.data) renderHelper.AddAll(car.Mesh.GetMesh());

            bool suppressHighlights = ToolAttributes.Contains(ToolAttribs.noHighlights);
            if (!suppressHighlights) DrawHighlights(time);

            //Draw node selectors
            stats.Nodes = World.Nodes.data.Count;
            foreach (var node in World.Nodes.data) {
                stats.Lanes += node.Lanes.Count;
                if (CheckNodes.Checked) renderHelper.AddAll(node.Mesh.GetMesh());
            }
                

            //Draw SelectorObjects
            renderHelper.AddAll(SelectorObjects);

            //If the add lane button is selected, draw it
            Mesh plusRenderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Add);
            if (MouseOver?.Tag is AddLaneSelection selection)
                RoadRenderer.CreateAddLane(selection, plusRenderBin, RoadTool.GetActualLaneSpec(this).Width, roadSegmentHighlightColor, 0.5f);

            //Render the snapping grid
            if (CheckSnap.Checked) renderHelper.AddAll(configuration.SnapGrid.Mesh.GetMesh());
            
            //Render ground with multiple planes
            var centerPos = renderManager.Camera.Position;
            Mesh grassBin = renderHelper.GetOrCreateRenderBinForced(Assets.Grass);
            RenderGround(centerPos, grassBin);

            //Render road tool
            configuration.Tool?.Draw(time);

            //Render marking points
            if (CheckPoints.Checked) {
                List<MarkingPointData> entries = [];

                foreach (RoadNode node in World.Nodes.data)
                    foreach (var lane in node.Lanes)
                        foreach (var laneEnd in new LaneEnd[] { lane.Front, lane.Rear })
                            foreach (var alignment in new float[] { 0, 1 }) entries.Add(new() { Anchor = laneEnd, Alignment = alignment });
                foreach (var entry in entries) MarkingRenderer.RenderMarkingPoint(entry, renderHelper);
            }

            //Render the render helper
            var tris = 0;
            var verts = 0;
            foreach (var bin in renderHelper.RenderBins.Values) {
                tris += (bin.Indices.Count) / 3;
                verts += bin.Vertices.Count;
            }
            stats.Triangles = tris;
            stats.Vertices = verts;
            stats.Materials = renderHelper.RenderBins.Count;

            renderManager.Render(renderHelper);

            Stats = stats;
        }

        private void RenderGround(Vector3 posoffset, Mesh renderBin) {
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
        private void GroundParallelogram(Mesh renderBin, Vector3 initialpos, Vector3 basepos, Vector3 xplus, Vector3 yplus, float scale) {
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
