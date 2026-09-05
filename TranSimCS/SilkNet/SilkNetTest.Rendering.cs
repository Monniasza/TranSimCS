using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    public partial class SilkNetTest {
        private void Render3D(RenderTarget target) {
            List<IObjMesh> meshes = [];
            MultiMesh mesh = new MultiMesh();

            //Add world contents
            meshes.AddRange(World.Nodes.data);
            meshes.AddRange(World.RoadSegments.data);
            meshes.AddRange(World.RoadSections.data);
            meshes.AddRange(World.Buildings.data);
            meshes.AddRange(World.Cars.data);

            //Draw highlights
            Mesh roadRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Road);

            var colors = Mode.SelectionColors();

            var nodecolor = colors.ObjectColor;
            var lanecolor = colors.ComponentColor;

            if ((MouseOver?.SelectedObj is RoadStrip strip)) {
                var fstag = strip.Bounds;
                LaneRangeMethods.GenerateLaneRangeMesh(fstag, roadRenderBin, nodecolor, 0.45f);
            }

            //If a road segment is selected, draw the selection
            if ((MouseOver?.Tag) is LaneStrip laneStrip) {
                // Draw the selected lane tag with a different color
                var laneTag = laneStrip.Tag();
                LaneRangeMethods.GenerateLaneRangeMesh(laneTag, roadRenderBin, lanecolor, 0.5f);
            }

            //Draw the selected road node
            var selectedObj = MouseOver?.Tag;
            HalfLane? laneEnd = null;
            if (selectedObj is IRoadElement roadElement && roadElement.GetLaneEnd() != null && roadElement.GetRoadStrip() == null)
                laneEnd = roadElement.GetLaneEnd();
            if (SelectNodes) foreach (var node in World.Nodes.data) {
                NodeRenderer.GenerateRoadNodeSelectionMesh(node, roadRenderBin, laneEnd);
            }

            //Render the tool
            Mode.Draw3D(target, mesh);

            //Create selectors
            MultiMesh visibleSelectors = new();
            MultiMesh invisibleSelectors = new();
            Mode.AddSelectors(invisibleSelectors, visibleSelectors);
            invisibleSelectors.AddAll(visibleSelectors);
            mesh.AddAll(visibleSelectors);
            World.TempSelectorsMesh.Value = invisibleSelectors;

            //Add the grass
            Mesh grassMesh = mesh.GetOrCreateRenderBinForced(Assets.Grass);
            if(Settings.ShowGround) InGameMenu.RenderGround(Vector3.Zero, grassMesh);

            //Draw the snapping grid
            if (SnappingEnabled) target.Draw(snappingGrid.Mesh.GetMesh());

            //Apply the day/night cycle
            var isDayNight = Settings.DayNightCycle;
            Vector4 dayVector = new(1, 1, 1, 1);
            Vector4 nightVector = new(0.2f, 0.2f, 0.5f, 1);
            Vector4 sunsetVector = new(1, 1, 0.5f, 1);

            LUT lut = new([
                new(-1, sunsetVector), new(0, sunsetVector), new(5, dayVector),
                new(25, dayVector), new(30, sunsetVector), new(33, nightVector),
                new(57, nightVector), new(60, sunsetVector), new(61, sunsetVector)
            ]);

            var seconds = World.DayTime;
            if (!isDayNight) seconds = 15;
            var radsPerSecond = MathF.PI / 30;

            var trig = MathF.SinCos(seconds * radsPerSecond);
            var sine = trig.Sin;
            var cosine = trig.Cos;

            var coefficient = GeometryUtils.Clamp(sine * 2, -1, 1);
            coefficient = (sine / 2) + 0.5f;
            var interpolatedDayNightVector = lut[seconds];
            RenderManager.AmbientColor.Value = interpolatedDayNightVector;

            //Render the sun
            var sunDistance = 10000f;
            var sunDiameter = 1000f;
            var pos = new Vector3(-cosine, sine, 0) * sunDistance;
            var normal = new Vector3(cosine, -sine, 0);
            var tangent = new Vector3(-sine, -cosine, 0) * sunDiameter;
            var lateral = new Vector3(0, 0, sunDiameter);
            var startingPoint = pos - (tangent + lateral) / 2;
            //var sunRenderBin = renderHelper.GetOrCreateRenderBinForced(Assets.White);
            var sunRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Sun);
            sunRenderBin.DrawParallelogram(startingPoint + RenderManager.Camera.Position.ToX0Z(), tangent, lateral, Colors.White);

            //Push meshes
            foreach (var element in meshes) element.GenerateGeometry(target);
            target.Draw(mesh);
        }
    }
}
