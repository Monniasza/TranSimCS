using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads;

namespace TranSimCS.Roads.Strip {
    public static class StripRenderer {

        public static void GenerateLaneStripMesh(LaneStrip laneStrip, MultiMesh renderer, float voffset = 0) {
            var tag = laneStrip.Tag();
            var (Left, Right) = RoadRenderer.GenerateSplines(tag, voffset); // Generate the splines for the left and right lanes

            var apshaltBin = renderer.GetOrCreateRenderBinForced(Assets.Asphalt);
            var leftPoints = GeometryUtils.GenerateSplinePoints(Left);
            var rightPoints = GeometryUtils.GenerateSplinePoints(Right);
            var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftPoints, rightPoints, GenerateLaneStripVertexGen(laneStrip.Spec));
            apshaltBin.DrawStrip(generatedVertStripPair);
            apshaltBin.AddTagsToLastTriangles((leftPoints.Length * 2) - 2, laneStrip);

            //Generate arrows
            float aoffset = 0.15f;
            var t = 0.5f;
            
            var avgspline = (Right + Left) / 2;
            var lpoint = Left[t];
            var rpoint = Right[t];
            var midpoint = (lpoint + rpoint) / 2;
            var tangent = avgspline.Tangential(t);
            
            tangent.Normalize();
            var fakebinormal = rpoint - lpoint;
            var width = Vector3.Cross(tangent, fakebinormal).Length();
            var normalfakebirnormal = Vector3.Normalize(fakebinormal);
            var nrm = Vector3.Cross(tangent, normalfakebirnormal);
            nrm.Normalize();

            var arrowWidth = width / 2;
            var displacement = tangent * width / 2;
            midpoint += nrm * aoffset;
            if (laneStrip.IsReverse()) displacement *= -1;

            var arrowBin = renderer.GetOrCreateRenderBinForced(Assets.Arrow);
            arrowBin.DrawLine(midpoint - displacement, midpoint + displacement, nrm, Color.White, arrowWidth);

            //Generate side-lines
        }

        public static VertexGen2<VertexPositionColorTexture> GenerateLaneStripVertexGen(LaneSpec spec) => GenerateLaneStripVertexGen(spec.Color);
        public static VertexGen2<VertexPositionColorTexture> GenerateLaneStripVertexGen(Color c) {
            (VertexPositionColorTexture, VertexPositionColorTexture) GenerateVertices(Vector3 l, Vector3 r, float distance, int index) {
                float mutualDistance = Vector3.Distance(l, r) / 2;
                return (
                    new VertexPositionColorTexture(l, c, new(-mutualDistance, distance)),
                    new VertexPositionColorTexture(r, c, new(mutualDistance, distance))
                );
            }
            return GenerateVertices;
        }
    }
}