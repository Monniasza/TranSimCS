using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using static TranSimCS.Geometry.GeometryUtils;

namespace TranSimCS.Roads {
    public static class SegmentRenderer {
        /// <summary>
        /// Generates the mesh for a road segment.
        /// </summary>
        /// <param name="connection">road segment</param>
        /// <param name="renderHelper">render helper</param>
        public static void GenerateRoadSegmentFullMesh(RoadStrip connection, MultiMesh renderHelper, float voffset = 0) {
            IRenderBin roadBin = renderHelper.GetOrCreateRenderBin(Assets.Road);
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
            IRenderBin finishBin = renderHelper.GetOrCreateRenderBin(texture);
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

            //Fill in unused areas
            
            //To check intersections, 
            
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