using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Polygons;
using static TranSimCS.Geometry.GeometryUtils;

namespace TranSimCS.Roads {
    public static class SegmentRenderer {
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentFullMesh(RoadStrip connection, MultiMesh renderHelper, float voffset = 0) {
            IRenderBin roadBin = renderHelper.GetOrCreateRenderBinForced(Assets.Road);
            foreach (var lane in connection.Lanes)
                roadBin.DrawModel(lane.GetMesh());

            //Draw the road finish
            var finish = connection.Finish;
            var texture = finish.subsurface.GetTexture();
            var height = finish.depth;
            var breadth = finish.depth * MathF.Tan(finish.angle);

            var splineFrame = connection.CalcSplineFrame();
            var bounds = connection.Bounds;

            var swidth = MathF.Abs(bounds.rightStart - bounds.leftStart);
            var ewidth = MathF.Abs(bounds.rightEnd - bounds.leftEnd);
            var avgWidth = (swidth + ewidth) / 2;

            var leftStart = Vector3.UnitX * bounds.leftStart;
            var rightStart = Vector3.UnitX * bounds.rightStart;
            var leftEnd = Vector3.UnitX * bounds.leftEnd;
            var rightEnd = Vector3.UnitX * bounds.rightEnd;
            var bottomLeft = -Vector3.UnitY * height - Vector3.UnitX * breadth;
            var bottomRight = -Vector3.UnitY * height + Vector3.UnitX * breadth;

            var leftTop = splineFrame.CreateFromStartEnd(leftStart, leftEnd);
            var rightTop = splineFrame.CreateFromStartEnd(rightStart, rightEnd);
            var leftDown = splineFrame.CreateFromStartEnd(leftStart + bottomLeft, leftEnd + bottomLeft);
            var rightDown = splineFrame.CreateFromStartEnd(rightStart + bottomRight, rightEnd + bottomRight);

            var leftTopPoints = GenerateSplinePoints(leftTop);
            var rightTopPoints = GenerateSplinePoints(rightTop);
            var leftDownPoints = GenerateSplinePoints(leftDown);
            var rightDownPoints = GenerateSplinePoints(rightDown);

            var sideLen = new Vector2(height, breadth).Length();

            var zeroFn = UniformTexturing.WithFixedU(0);

            var sideLenFn = UniformTexturing.WithFixedU(sideLen);
            var avgWidthFn = UniformTexturing.WithFixedU(avgWidth);

            var leftPointsL = UniformTexturing.UniformTextured(leftDownPoints, zeroFn);
            var leftPointsR = UniformTexturing.UniformTextured(leftTopPoints, sideLenFn);
            var rightPointsL = UniformTexturing.UniformTextured(rightTopPoints, zeroFn);
            var rightPointsR = UniformTexturing.UniformTextured(rightDownPoints, sideLenFn);
            var bottomPointsL = UniformTexturing.UniformTextured(rightDownPoints, zeroFn);
            var bottomPointsR = UniformTexturing.UniformTextured(leftDownPoints, avgWidthFn);

            //Draw the strips
            IRenderBin finishBin = renderHelper.GetOrCreateRenderBinForced(texture);
            finishBin.DrawStrip(leftPointsL, leftPointsR);
            finishBin.DrawStrip(rightPointsL, rightPointsR);
            finishBin.DrawStrip(bottomPointsL, bottomPointsR);

            //Draw the endcaps
            var leftUpStartPos = leftTopPoints[0];
            var rightUpStartPos = rightTopPoints[0];
            var rightDownStartPos = rightDownPoints[0];
            var leftDownStartPos = leftDownPoints[0];
            GenerateEndCap(leftUpStartPos, rightUpStartPos, rightDownStartPos, leftDownStartPos, swidth, height, breadth, finishBin);

            var leftUpEndPos = leftTopPoints.Last();
            var rightUpEndPos = rightTopPoints.Last();
            var rightDownEndPos = rightDownPoints.Last();
            var leftDownEndPos = leftDownPoints.Last();
            GenerateEndCap(rightUpEndPos, leftUpEndPos, leftDownEndPos, rightDownEndPos, swidth, height, breadth, finishBin);

            //If the road is only 1 lane, do not render the islands
            if (connection.Lanes.Count < 2) return;

            //Find fill polygons for lane strips
            var laneRanges = new List<LaneRange>();
            var fstag = connection.FullSizeTag();
            if (fstag == null) return;
            laneRanges.Add(fstag.Value);
            laneRanges.AddRange(connection.Lanes.Select(lane => lane.Tag));

            List<Polygon> polygons = [];
            
            int i = -1;
            foreach(var lane in laneRanges) {
                i++;
                var strip = RoadRenderer.GenerateSplines(lane);
                var leftSpline = strip.Item1;
                var rightSpline = strip.Item2.Inverse();
                var leftPoints = GeometryUtils.GenerateSplinePoints(leftSpline);
                var rightPoints = GeometryUtils.GenerateSplinePoints(rightSpline);
                var mergedPoints = leftPoints.Concat(rightPoints);
                var mergedPoint = mergedPoints.First();
                Debug.Print($"Point: {mergedPoint}");
                var unraveledPoints = mergedPoints.Select(pt => splineFrame.UnTransform(pt));
                var path = FlattenPath(unraveledPoints);
                var polygon = new Polygon(path, FillRule.EvenOdd);
                polygons.Add(polygon);
            }

            //Create the global polygon
            var globalPolygon = polygons[0];
            var lanePolygons = polygons.Skip(1).ToArray();
            Debug.Print($"{lanePolygons.Length} lane polygons");
            //Slightly enlarge the lane polygons to prevent degeneration
            lanePolygons = lanePolygons.Select(poly => poly.Offset(0.000001)).ToArray();

            //Perform the separation logic
            var islandsPoly = globalPolygon.SubtractMore(lanePolygons);
            Debug.Print($"Island: {islandsPoly.path.Count}");

            //Calculate length of the road
            var lengthL = GeometryUtils.CountLength(leftTopPoints);
            var lengthR = GeometryUtils.CountLength(rightTopPoints);
            var length = lengthL + lengthR;

            //Back-transform the paths
            foreach(var path in islandsPoly.path) {
                DrawIsland(Surface.Tiles, Surface.Concrete, renderHelper, splineFrame, path, 0.5f, length);
            }

        }

        public static PathD FlattenPath(IEnumerable<Vector3> points) => new PathD(points.Select(v => new PointD(v.X, v.Z)));

        public static void DebugPolygon(MultiMesh mesh, Polygon polygon, Color c, Vector3 position, float stretch = 50) {
            //Debug.Print("DebugPolygon");
            var renderBin = mesh.GetOrCreateRenderBinForced(Assets.Road);
            foreach (var loop in polygon.path) {
                //Debug.Print("DebugPolygon loop");
                for (int i = 0; i < loop.Count; i++) {   
                    var prev = loop[i];
                    var next = loop[(i + 1) % loop.Count];
                    var p1 = UnmapPolyPoint(prev, stretch);
                    var p2 = UnmapPolyPoint(next, stretch);
                    //Debug.Print($"DebugPolygon vert {i} @ {p1}");
                    renderBin.DrawLine(p1, p2, Vector3.UnitY, c);
                }
            }
        }
        public static Vector3 UnmapPolyPoint(PointD p, float stretch = 50) {
            float x = (float)p.x - 200;
            float y = 0.1f;
            float z = (float)p.y;
            return new Vector3(x, y, (z * stretch) - 60);
        }

        public static void DrawIsland(Surface surface, Surface sideSurface, MultiMesh mesh, SplineFrame frm, PathD path, float h, float stretch) {
            if (Clipper.Area(path) < 0) path.Reverse();
            var retransformedPointsUp = Retransform(frm, path, h);

            if (DebugOptions.DebugIslands) {
                var retransformedPointsHighUp = Retransform(frm, path, h * 2).ToArray();
                var roadBin = mesh.GetOrCreateRenderBinForced(Assets.Road);
                for (int i = 0; i < retransformedPointsHighUp.Length; i++) {
                    var prev = retransformedPointsHighUp[i];
                    var next = retransformedPointsHighUp[(i + 1) % retransformedPointsHighUp.Length];
                    roadBin.DrawLine(prev, next, Vector3.UnitY, Color.Red);
                }
            }

            var averagePosition = retransformedPointsUp.Aggregate((x, y) => x + y);

            if(mesh.TryGetOrCreateRenderBin(sideSurface.GetTexture(), out var sideRenderBin)) {
                var retransformedPoints = Retransform(frm, path, 0);
                var retransformedPointsCyclic = retransformedPoints.Append(retransformedPoints.First()).ToArray();
                var retransformedPointsUpCyclic = retransformedPointsUp.Append(retransformedPointsUp.First()).ToArray();
                var texturedStrip = UniformTexturing.UniformTexturedTwin(retransformedPointsCyclic, retransformedPointsUpCyclic, UniformTexturing.PairStrip());
                sideRenderBin.DrawStrip(texturedStrip.Item2, texturedStrip.Item1);
            }
            if(mesh.TryGetOrCreateRenderBin(surface.GetTexture(), out var topRenderBin)) {
                //Triangulate first in 2D
                var transformedPath = path.Select(p => new PointD(p.x, p.y * stretch)).ToArray();
                //transformedPath.Reverse();
                //var triangulation = Triangulate2D.TriangulatePolygon(transformedPath).ToArray();
                var triangulation = Triangulate2D.LongitudinalTriangulate(transformedPath);
                var requiredTriCount = (path.Count - 2) * 3;
                Debug.Print($"Requested idx count: {requiredTriCount} triangulation: {triangulation.Length}");
                

                //Fill the top
                var vertices = retransformedPointsUp.Select(GeometryUtils.CreateVertex).ToArray();
                RenderUtil.InvertNormals(triangulation);
                ((IRenderBin)topRenderBin).DrawModel(vertices, triangulation);
            }
        }
        public static IEnumerable<Vector3> Retransform(SplineFrame frame, IEnumerable<PointD> pts, float z = 0) {
            return pts.Select(pt => frame.Transform(new((float)pt.x, z, (float)pt.y)));
        }

        public static void GenerateEndCap(Vector3 ul, Vector3 ur, Vector3 dr, Vector3 dl, float width, float height, float expand, IRenderBin mesh) {
            var p1 = new VertexPositionColorTexture(ul, Color.White, new(0, 0));
            var p2 = new VertexPositionColorTexture(ur, Color.White, new(width, 0));
            var p3 = new VertexPositionColorTexture(dr, Color.White, new(width + expand, -height));
            var p4 = new VertexPositionColorTexture(dl, Color.White, new(-expand, -height));
            mesh.DrawQuad(p1, p2, p3, p4);
        }
    }
}