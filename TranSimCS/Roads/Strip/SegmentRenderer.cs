using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Clipper2Lib;
using LanguageExt.ClassInstances;
using LanguageExt.UnitsOfMeasure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using TranSimCS.Debugging;
using TranSimCS.Geometry;
using TranSimCS.Geometry.SplineFrames;
using TranSimCS.Model;
using TranSimCS.Polygons;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;

namespace TranSimCS.Roads.Strip {
    public static class SegmentRenderer {
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentFullMesh(RoadStrip connection, MultiMesh renderHelper, float voffset = 0) {
            if(connection == null || connection.Lanes.Count == 0) return;

            Mesh roadBin = renderHelper.GetOrCreateRenderBinForced(Assets.Road);
            foreach (var lane in connection.Lanes) {
                renderHelper.AddAll(lane.GetMesh());
            }
                
            //Draw the road finish
            var finish = connection.Finish;
            var texture = finish.subsurface.GetTexture();
            var height = finish.depth;
            var breadth = finish.depth * MathF.Tan(finish.angle);

            LaneRange topRange = connection.FullSizeTag();
            LaneRange bottomRange = new LaneRange(
                connection,
                new(topRange.startRange.Min - breadth, topRange.startRange.Max + breadth),
                new(topRange.endRange.Min - breadth, topRange.endRange.Max + breadth)
            );

            var (leftTop, rightTop) = RoadRenderer.GenerateSplines(topRange);
            var (leftDown, rightDown) = RoadRenderer.GenerateSplines(bottomRange, -height);

            var splineFrame = connection.SplineFrame;
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
            Mesh finishBin = renderHelper.GetOrCreateRenderBinForced(texture);
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

            //Calculate length of the road
            var lengthL = CountLength(leftTopPoints);
            var lengthR = CountLength(rightTopPoints);
            var length = lengthL + lengthR;

            //If this segment is single-ended, draw the inner island
            if (connection.IsSingleEnded()) 
                RenderSingleEndedInnerCircle(connection, renderHelper, length);
            
            //If the road is only 1 lane, do not render the islands
            if (connection.Lanes.Count >= 2)
                RenderRoadSegmentPolygons(connection, renderHelper, length);
        }

        public static void RenderSingleEndedInnerCircle(RoadStrip connection, MultiMesh renderHelper, float length) {
            //Conpute the limits of the road segment
            Range<float> leftBounds = default, rightBounds = default;
            foreach (LaneStrip lane in connection.Lanes) {
                var boundsA = lane.StartLane.Range();
                var boundsB = lane.EndLane.Range();
                var centerA = boundsA.Middle();
                var centerB = boundsB.Middle();
                if (centerA > centerB) DataUtil.Swap(ref boundsA, ref boundsB);
                leftBounds = leftBounds.Union(boundsA);
                rightBounds = rightBounds.Union(boundsB);
            }

            float a = leftBounds.Max;
            float b = rightBounds.Min;
            
            if (connection.StartNode.End == NodeEnd.Backward)
                (a, b) = (-b, -a);
            var width = b - a;
            var d = width * 0.6666666666666666666666666667f;
            var spline = new Bezier3(
                new(b, 0, 0), new(b, 0, d), new(a, 0, d), new(a, 0, 0)
            );
            var points = GeometryUtils.GenerateSplinePoints(spline);
            var refframe = connection.StartNode.CalcReferenceFrame();
            var nodeSplineFrame = new SplineFrame();
            nodeSplineFrame.CenterSpline = new(refframe.O, refframe.O + refframe.Z);
            nodeSplineFrame.XPlusSpline = new(refframe.X);
            nodeSplineFrame.YPlusSpline = new(refframe.Y);

            var pointsFlat = FlattenPath(points);
            DrawIsland(Surface.Grass, Surface.Concrete, renderHelper, nodeSplineFrame, new PathD(pointsFlat), 0.1f, 1);
        }

        public static void RenderRoadSegmentPolygons(RoadStrip connection, MultiMesh renderHelper, float length) {
            var splineFrame = connection.SplineFrame.ConvertConventions(connection.StartNode.End, connection.EndNode.End);

            //Find fill polygons for lane strips
            var laneRanges = new List<LaneRange>();
            var fstag = connection.FullSizeTag();
            laneRanges.Add(fstag);
            laneRanges.AddRange(connection.Lanes.Select(lane => lane.Tag()));

            List<Polygon> polygons = [];
            foreach (var lane in laneRanges) {
                //Widen the lane range
                float dwidth = 0.001f;
                var widened = lane;
                widened.startRange = new(widened.startRange.Min - dwidth, widened.startRange.Max + dwidth);
                widened.endRange = new(widened.endRange.Min - dwidth, widened.endRange.Max + dwidth);
                widened.startRange = connection.StartNode.End.ConvertConventions(widened.startRange);
                widened.endRange = connection.EndNode.OppositeEnd.End.ConvertConventions(widened.endRange);
                var pos1L = widened.startRange.Min;
                var pos1R = widened.startRange.Max;
                var pos2L = widened.endRange.Min;
                var pos2R = widened.endRange.Max;
                int numberOfPoints = 32;
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

        public static void DrawIsland(Surface surface, Surface sideSurface, MultiMesh mesh, SplineFrame frm, PathD path, float h, float stretch) {
            var area = Clipper.Area(path);
            if (area < 0) {
                path.Reverse();
                area *= -1;
            }
            //Reject polygons with a tiny width
            var perimeter = Polygon.Perimeter(path);
            var avgWidth = area / perimeter;
            if (avgWidth < 0.01) return;

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
                var texturedStrip = UniformTexturing.UniformTexturedTwin(retransformedPointsCyclic, retransformedPointsUpCyclic, UniformTexturing.PairStrip());
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

        public static IEnumerable<Vector3> Retransform(SplineFrame frame, IEnumerable<PointD> pts, float z = 0) {
            return pts.Select(pt => frame.TransformNodeConvention(new((float)pt.x, z, (float)pt.y)));
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