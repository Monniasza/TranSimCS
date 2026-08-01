using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Polygons;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.StripData;
using TranSimCS.Setting;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;

namespace TranSimCS.Roads.Strip {
    public static class SegmentRenderer {
        /*
         * TODO: Works poorly on single-ended road nodes.
         * Split into 2 strategies, each with their own algorithms
         */


        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentFullMesh(RoadStripData connection, MultiMesh renderHelper) {
            if(connection == null || connection.LaneConnections.Count == 0) return;

            //Clearly needs 2 strategies: one for previews and other for real roads.

            foreach (var lane in connection.LaneConnections) {
                renderHelper.AddAll(lane.Value.Mesh);
            }

            //Draw the road finish, Calculate length of the road
            var length = GenerateRoadSegmentFinish(connection, renderHelper);            

            //If this segment is single-ended, draw the inner island
            if (connection.IsSingleEnded) 
                RenderSingleEndedInnerCircle(connection, renderHelper, length);
            
            //If the road is only 1 lane, do not render the islands
            if (connection.LaneConnections.Count >= 2)
                RenderRoadSegmentPolygons(connection, renderHelper, length);
        }

        public static float GenerateRoadSegmentFinish(RoadStripData connection, MultiMesh renderHelper) {
            var accuracy = Settings.RoadAccuracy;

            //Calculate road length
            var topRange = connection.Bounds;
            var (leftTop, rightTop) = LaneRangeMethods.GenerateSplines(topRange, connection);
            var lengthL = CountLength(leftTop);
            var lengthR = CountLength(rightTop);
            var length = lengthL + lengthR;

            //Draw the road finish
            var finish = connection.Finish;
            var texture = finish.subsurface.GetTexture();
            var height = finish.depth;
            var breadth = finish.depth * MathF.Tan(finish.angle);

            if (texture == null) return length;
            var bottomRange = new DualRange(
                new(topRange.startRange.Min - breadth, topRange.startRange.Max + breadth),
                new(topRange.endRange.Min - breadth, topRange.endRange.Max + breadth)
            );
            var (leftDown, rightDown) = LaneRangeMethods.GenerateSplines(bottomRange, connection, -height);

            var splineFrame = connection.OrthodistantBasis;
            var bounds = connection.Bounds;

            var swidth = bounds.startRange.Width();
            var ewidth = bounds.endRange.Width();
            var avgWidth = (swidth + ewidth) / 2;

            var sideLen = new Vector2(height, breadth).Length();

            var zeroFn = UniformTexturing.WithFixedU(0);
            var sideLenFn = UniformTexturing.WithFixedU(sideLen);
            var avgWidthFn = UniformTexturing.WithFixedU(avgWidth);

            var leftPointsL = UniformTexturing.UniformTextured(leftDown, zeroFn);
            var leftPointsR = UniformTexturing.UniformTextured(leftTop, sideLenFn);
            var rightPointsL = UniformTexturing.UniformTextured(rightTop, zeroFn);
            var rightPointsR = UniformTexturing.UniformTextured(rightDown, sideLenFn);
            var bottomPointsL = UniformTexturing.UniformTextured(rightDown, zeroFn);
            var bottomPointsR = UniformTexturing.UniformTextured(leftDown, avgWidthFn);

            //Draw the strips
            Mesh finishBin = renderHelper.GetOrCreateRenderBinForced(texture.Value);
            finishBin.DrawStrip(leftPointsL, leftPointsR);
            finishBin.DrawStrip(rightPointsL, rightPointsR);
            finishBin.DrawStrip(bottomPointsL, bottomPointsR);

            //Draw the endcaps
            var leftUpStartPos = leftTop[0];
            var rightUpStartPos = rightTop[0];
            var rightDownStartPos = rightDown[0];
            var leftDownStartPos = leftDown[0];
            GenerateEndCap(leftUpStartPos, rightUpStartPos, rightDownStartPos, leftDownStartPos, swidth, height, breadth, finishBin);

            var leftUpEndPos = leftTop.Last();
            var rightUpEndPos = rightTop.Last();
            var rightDownEndPos = rightDown.Last();
            var leftDownEndPos = leftDown.Last();
            GenerateEndCap(rightUpEndPos, leftUpEndPos, leftDownEndPos, rightDownEndPos, swidth, height, breadth, finishBin);

            return length;
        }

        public static void RenderSingleEndedInnerCircle(RoadStripData connection, MultiMesh renderHelper, float length) {
            var accuracy = Settings.RoadAccuracy;
            //Conpute the limits of the road segment
            Range<float> leftBounds = default, rightBounds = default;
            foreach (var row in connection.LaneConnections) {
                var lane = row.Value;
                var boundsA = lane.StartNode.Bounds;
                var boundsB = lane.EndNode.Bounds;
                var centerA = boundsA.Middle();
                var centerB = boundsB.Middle();
                if (centerA > centerB) DataUtil.Swap(ref boundsA, ref boundsB);
                leftBounds = leftBounds.Union(boundsA);
                rightBounds = rightBounds.Union(boundsB);
            }

            float a = leftBounds.Max;
            float b = rightBounds.Min;
            var points = RoadStrip.GenerateSpline(connection, b, a);
            var refframe = connection.StartPos.CalcReferenceFrame();
            var nodeSplineFrame = new OrthodistantBasis();
            nodeSplineFrame.NormalSpline = new(refframe.Y);
            nodeSplineFrame.ReferenceSpline = new(refframe.O, refframe.O+refframe.X);

            var pointsFlat = FlattenPath(points);
            DrawIsland(Surface.Grass, Surface.Concrete, renderHelper, OrthodistantBasis.Identity, new PathD(pointsFlat), 0.1f, 1);
        }

        public static void RenderRoadSegmentPolygons(RoadStripData connection, MultiMesh renderHelper, float length) {
            var splineFrame = connection.OrthodistantBasis;

            //Find fill polygons for lane strips
            var laneRanges = new List<DualRange>();
            var fstag = connection.Bounds;
            laneRanges.Add(fstag);
            laneRanges.AddRange(connection.LaneStripExtents.Groups.Select(x => x.Bounds));

            List<Polygon> polygons = [];
            foreach (var lane in laneRanges) {
                //Widen the lane range
                float dwidth = 0.001f;
                var widened = lane;
                widened.startRange = new(widened.startRange.Min - dwidth, widened.startRange.Max + dwidth);
                widened.endRange = new(widened.endRange.Min - dwidth, widened.endRange.Max + dwidth);
                var pos1L = widened.startRange.Min;
                var pos1R = widened.startRange.Max;
                var pos2L = -widened.endRange.Max;
                var pos2R = -widened.endRange.Min;
                int numberOfPoints = Settings.RoadAccuracy;
                var path = new PathD();
                for(int i = 0; i < numberOfPoints; i++) {
                    var t = (float)i / (numberOfPoints-1);
                    path.Add(new(MathHelper.SmoothStep(pos1R, pos2R, t), t * length));
                }
                for (int i = 0; i < numberOfPoints; i++) {
                    var t = (float)i / (numberOfPoints - 1);
                    t = 1 - t;
                    path.Add(new(MathHelper.SmoothStep(pos1L, pos2L, t), t * length));
                }
                var polygon = new Polygon(path, FillRule.EvenOdd);
                polygons.Add(polygon);
            }

            //Create the global polygon
            var globalPolygon = polygons[0];
            var lanePolygons = polygons.Skip(1).ToArray();

            //Perform the separation logic
            var islandsPoly = globalPolygon.SubtractMore(lanePolygons);

            //Back-transform the paths
            foreach (var path in islandsPoly.path)
                DrawIsland(Surface.Grass, Surface.Concrete, renderHelper, splineFrame, path, 0.1f, length);
        }

        public static PathD FlattenPath(IEnumerable<Vector3> points) => new PathD(points.Select(v => new PointD(v.X, v.Z)));

        public static void DrawIsland(Surface surface, Surface sideSurface, MultiMesh mesh, OrthodistantBasis frm, PathD path, float h, float stretch) {
            var area = Clipper.Area(path);
            if (area < 0) {
                path.Reverse();
                area *= -1;
            }
            //Reject polygons with a tiny width
            var perimeter = Polygon.Perimeter(path);
            var avgWidth = area / perimeter;
            if (area < 0.0001 || avgWidth < 0.01) return;

            var untransformedPath = path.Select(p => new PointD(p.x, p.y / stretch)).ToArray();

            if (DebugOptions.DebugIslands) {
                var retransformedPointsHighUp = Retransform(frm, untransformedPath, h * 2).ToArray();
                var roadBin = mesh.GetOrCreateRenderBinForced(Assets.Road);
                for (int i = 0; i < retransformedPointsHighUp.Length; i++) {
                    var prev = retransformedPointsHighUp[i];
                    var next = retransformedPointsHighUp[(i + 1) % retransformedPointsHighUp.Length];
                    roadBin.DrawLine(prev, next, Vector3.UnitY, Color.Red);
                }
            }

            var retransformedPointsUp = Retransform(frm, untransformedPath, h);
            if (mesh.TryGetOrCreateRenderBin(sideSurface.GetTexture(), out var sideRenderBin)) {
                var retransformedPoints = Retransform(frm, untransformedPath, 0);
                var retransformedPointsCyclic = retransformedPoints.Append(retransformedPoints.First()).ToArray();
                var retransformedPointsUpCyclic = retransformedPointsUp.Append(retransformedPointsUp.First()).ToArray();
                var texturedStrip = UniformTexturing.UniformTexturedTwin(retransformedPointsCyclic, retransformedPointsUpCyclic, UniformTexturing.GenerateLaneStripVertexGen(Color.White));
                sideRenderBin.DrawStrip(texturedStrip.Item2, texturedStrip.Item1);
            }
            if(mesh.TryGetOrCreateRenderBin(surface.GetTexture(), out var topRenderBin)) {
                //Triangulate first in 2D
                var triangulation = Triangulate2D.LongitudinalTriangulate(path.ToArray());
                var requiredTriCount = (path.Count - 2) * 3;
                Debug.Print($"Requested idx count: {requiredTriCount} triangulation: {triangulation.Length}");

                //Fill the top
                var vertices = retransformedPointsUp.Select(CreateVertex).ToArray();
                RenderUtil.InvertNormals(triangulation);
                ((Mesh)topRenderBin).DrawModel(vertices, triangulation);
            }
        }

        public static IEnumerable<Vector3> Retransform(OrthodistantBasis frame, IEnumerable<PointD> pts, float z = 0) {
            return pts.Select(pt => RetransformOne(frame, pt, z));
        }
        public static Vector3 RetransformOne(OrthodistantBasis frame, PointD vector, float z = 0){
            var offsetVector = new Vector3((float)vector.x, z, 0);
            return frame.SamplePosition((float)vector.y, offsetVector, offsetVector);
        }

        public static void GenerateEndCap(Vector3 ul, Vector3 ur, Vector3 dr, Vector3 dl, float width, float height, float expand, Mesh mesh) {
            var p1 = new VertexPositionColorTexture(ul, Color.White, new(0, 0));
            var p2 = new VertexPositionColorTexture(ur, Color.White, new(width, 0));
            var p3 = new VertexPositionColorTexture(dr, Color.White, new(width + expand, -height));
            var p4 = new VertexPositionColorTexture(dl, Color.White, new(-expand, -height));
            mesh.DrawQuad(p1, p2, p3, p4);
        }
    }
}