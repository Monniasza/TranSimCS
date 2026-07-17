using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using MadWorldNL.EarCut.Logic;
using TranSimCS.Polygons;

namespace TranSimCS.Geometry {
    public struct PathsDTriangulation : IEquatable<PathsDTriangulation> {
        public PointD[] points;
        public int[] triangles;

        public PathsDTriangulation(PointD[] points, int[] triangles) {
            this.points = points;
            this.triangles = triangles;
        }

        public static PathsDTriangulation Triangulate(Polygon polygon) {
            var extractedComponents = ExtractPolygonIslands.ExtractIslands(polygon.path, polygon.fillRule);
            int maxTriangles = 0;
            var pointsCount = 0;
            foreach(var island in extractedComponents) {
                int verts = island.Paths.Sum(x => x.Count);
                int holes = island.Paths.Count - 1;
                maxTriangles += verts + 2 * holes - 2;
                pointsCount += verts;
            }

            PointD[] points = new PointD[pointsCount];
            List<int> triangles = new();

            int pointIndex = 0;

            foreach (var island in extractedComponents) {
                int verts = island.Paths.Sum(x => x.Count);
                double[] transformedVertices = new double[verts * 2];
                int indexInTransformedVertices = 0;
                int holes = island.Paths.Count - 1;
                var holesArray = new int[holes];
                int indexInHolesArray = 0;

                //Swap the hole into the first slot
                PathD[] paths = island.Paths.ToArray();
                DataUtil.Swap(paths, 0, island.OuterLoopIndex);

                for (int i = 0; i < island.Paths.Count; i++) {
                    if (Clipper.Area(paths[i]) < 0 ^ i > 0) paths[i].Reverse();

                    var path = paths[i];
                    if (i > 0) holesArray[indexInHolesArray++] = indexInTransformedVertices / 2;
                    for (int j = 0; j < path.Count; j++) {
                        var point = path[j];
                        transformedVertices[indexInTransformedVertices++] = point.x;
                        transformedVertices[indexInTransformedVertices++] = point.y;
                    }
                }
                Debug.Assert(indexInHolesArray == holesArray.Length, "Not all holes filled");
                Debug.Assert(indexInTransformedVertices == transformedVertices.Length, "Not all vertices created");

                var triangulation = EarCut.Tessellate(transformedVertices, holesArray, 2);

                //Copy points into top level arrays
                Debug.Assert(triangulation != null, "Triangulation failed");
                for(int i = 0; i < triangulation.Count; i++) 
                    triangles.Add(triangulation[i] + pointIndex);
                foreach (var path in paths) foreach (var point in path) points[pointIndex++] = point;
            }

            //Assert completeness
            Debug.Assert(pointIndex == points.Length, "Not all points generated");
            Debug.Assert(triangles.Count <= (maxTriangles*3), "Too many indices generated");

            return new PathsDTriangulation(points, triangles.ToArray());
        }

        public override bool Equals(object? obj) {
            return obj is PathsDTriangulation triangulation && Equals(triangulation);
        }

        public bool Equals(PathsDTriangulation other) {
            return EqualityComparer<PointD[]>.Default.Equals(points, other.points) &&
                   EqualityComparer<int[]>.Default.Equals(triangles, other.triangles);
        }

        public override int GetHashCode() {
            return System.HashCode.Combine(points, triangles);
        }

        public static bool operator ==(PathsDTriangulation left, PathsDTriangulation right) {
            return left.Equals(right);
        }

        public static bool operator !=(PathsDTriangulation left, PathsDTriangulation right) {
            return !(left == right);
        }
    }
}
