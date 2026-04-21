using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Polygons;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Section {
    public static partial class RoadSurfaceGenerator {
        [Flags]
        public enum RoadSurfaceFlags {
            None = 0,
            Degenerate = 1,
            All = -1,
        }
        public struct RoadSectionClassifications {
            public RoadSurfaceFlags Flags;
            public WorkingPlane WorkingPlane;
            public float NormalWeight;
            public Bezier3[] GeneratedBorder;
            public Vector3[] GeneratedBorderPoints;
            public bool IsDegenerate;
        }

        public static RoadSectionClassifications RunHeuristics(RoadSection section) {
            RoadSectionClassifications result = new();

            //Heuristic 1: working plane
            Vector3 PositionSum = Vector3.Zero;
            Vector3 NormalSum = Vector3.Zero;
            foreach (var node in section.Nodes) {
                var centerPos = node.CenterPosition;
                var normal = node.CalcReferenceFrame().Y;
                PositionSum += centerPos;
                NormalSum += normal;
            }
            PositionSum /= section.Nodes.Count;
            var normalWeight = NormalSum.LengthSquared;
            NormalSum.Normalize();
            var facingFwdVector = (section.Nodes[0].CenterPosition - PositionSum).Orthogonalize(NormalSum);
            facingFwdVector.Normalize();
            WorkingPlane workingPlane = new();
            workingPlane.O = PositionSum;
            workingPlane.Y = facingFwdVector;
            workingPlane.X = Vector3.Cross(facingFwdVector, NormalSum).Normalized();

            //Heuristic 2: boundary generation
            Bezier3[] borders = new Bezier3[section.Nodes.Count];
            for(int i = 0; i < section.Nodes.Count; i++) {
                var node = section.Nodes[i];
                var nextNode = section.Nodes[(i + 1) % section.Nodes.Count];
                var startRef = node.CalcReferenceFrame();
                var endRef = node.CalcReferenceFrame();
                var startPos = startRef.O + startRef.X * node.Bounds().X;
                var endPos = endRef.O + endRef.X * nextNode.Bounds().Y;
                var startTangent = startRef.Z;
                var endTangent = endRef.Z;
                Bezier3 nextBorder = GeometryUtils.GenerateJoinSpline(startPos, endPos, startTangent, endTangent);
                borders[i] = nextBorder;
            }
            result.GeneratedBorder = borders;

            int nodesPerSpline = 17;
            Vector3[] generatedBorderPoints = new Vector3[nodesPerSpline * section.Nodes.Count];
            for (int i = 0; i < section.Nodes.Count; i++) {
                var startNodesIndex = i * nodesPerSpline;
                Vector3[] thisPoints = GeometryUtils.GenerateSplinePoints(borders[i], nodesPerSpline);
                Array.Copy(thisPoints, 0, generatedBorderPoints, startNodesIndex, nodesPerSpline);
            }

            //Heuristic 3: boundary classification
            PointD[] projectedPoints = generatedBorderPoints.Select(x => workingPlane.Project(x).ToPointD()).ToArray();
            Polygon borderPolygon = new Polygon(new PathD(projectedPoints), FillRule.EvenOdd);
            bool isValid = borderPolygon.IsPolygonValid();
            if (!isValid) result.Flags |= RoadSurfaceFlags.Degenerate;

            //Algorithm 1: if the node is degenerate, make the path a concave polygon
            if (!isValid) {
                var points4degenerated = new Vector3[section.Nodes.Count * 2];
                for (int i = 0; i <= section.Nodes.Count; i++) {
                    var node = section.Nodes[i];
                    var rf = node.CalcReferenceFrame();
                    var bounds = node.Bounds();
                    points4degenerated[ i * 2     ] = rf.O + rf.X * bounds.X;
                    points4degenerated[(i * 2) + 1] = rf.O + rf.X * bounds.Y;
                }
                result.GeneratedBorderPoints = points4degenerated;
            }

            return result;
        }

        public static bool IsPolygonValid(this Polygon borderPolygon) {
            if (borderPolygon.Area() < 0.001) return false;
            var simplified = borderPolygon.Simplify(0.001);
            //Check if both paths are approximately equal
            int length = borderPolygon.path.Count;
            int simplifiedLength = simplified.path.Count;
            if(length != simplifiedLength) return false;
            for (int i = 0; i < simplifiedLength; i++) {
                var basePath = borderPolygon.path[i];
                var simplifiedPath = simplified.path[i];
                int innerLengthBase = basePath.Count;
                int innerLengthSimplified = simplifiedPath.Count;
                if(innerLengthBase != innerLengthSimplified) return false;
                for (int j = 0; j < innerLengthSimplified; j++) {
                    var basePoint = basePath[j];
                    var simplifiedPoint = simplifiedPath[j];
                    if(!Clipper.PointsNearEqual(basePoint, simplifiedPoint, 0.0001)) return false;
                }
            }
            return true;
        }
    }
}
