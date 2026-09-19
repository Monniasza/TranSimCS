using System;
using System.Numerics;
using TranSimCS;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.SilkNet;
using Xunit;

namespace TranSimCSTests {
    public class TestProjectOnto {
        private static Mesh FlatQuadTarget() {
            //Unit quad on the y=0 plane, split into 2 triangles sharing the (0,0)-(1,1) diagonal
            var mesh = new Mesh();
            mesh.AddVertex(new Vertex(new(0, 0, 0), Colors.White, new(0, 0)));
            mesh.AddVertex(new Vertex(new(1, 0, 0), Colors.White, new(1, 0)));
            mesh.AddVertex(new Vertex(new(1, 0, 1), Colors.White, new(1, 1)));
            mesh.AddVertex(new Vertex(new(0, 0, 1), Colors.White, new(0, 1)));
            mesh.AddIndices([0, 1, 2, 0, 2, 3]);
            return mesh;
        }

        private static Mesh QuadProjection(float y, float min = 0.25f, float max = 0.75f) {
            var mesh = new Mesh();
            mesh.AddVertex(new Vertex(new(min, y, min), Colors.White, new(min, min)));
            mesh.AddVertex(new Vertex(new(max, y, min), Colors.White, new(max, min)));
            mesh.AddVertex(new Vertex(new(max, y, max), Colors.White, new(max, max)));
            mesh.AddVertex(new Vertex(new(min, y, max), Colors.White, new(min, max)));
            mesh.AddIndices([0, 1, 2, 0, 2, 3]);
            return mesh;
        }

        [Fact]
        public void CoplanarProjectionDoesNotThrow() {
            var target = FlatQuadTarget();
            var projection = QuadProjection(0);
            var result = projection.ProjectOnto(target, Vector3.UnitY);
            Assert.All(result.Vertices, v => Assert.True(MathF.Abs(v.Position.Y) < 1e-4f));
        }

        [Fact]
        public void VertexAboveSurfaceProjectsWithBehindRange() {
            var target = FlatQuadTarget();
            var projection = QuadProjection(0.05f);
            var result = projection.ProjectOnto(target, Vector3.UnitY, float.PositiveInfinity, -10);
            Assert.All(result.Vertices, v => Assert.True(MathF.Abs(v.Position.Y) < 1e-4f));
        }

        [Fact]
        public void HitJustPastTriangleEdgeIsAccepted() {
            var target = FlatQuadTarget();
            //Vertex 1e-7 beyond the x=1 edge of the quad - inside the triangle plane, outside the strict barycentric bounds
            var projection = new Mesh(null, [
                new Vertex(new(1.0000001f, 0, 0.5f), Colors.White, new(1, 0.5f)),
            ], [0, 0, 0]);
            var result = projection.ProjectOnto(target, Vector3.UnitY);
            Assert.True(MathF.Abs(result.Vertices[0].Position.Y) < 1e-4f);
        }

        [Fact]
        public void MissedVertexIsKeptUnprojected() {
            var target = FlatQuadTarget();
            var projection = new Mesh(null, [
                new Vertex(new(5, 3, 5), Colors.White, new(5, 5)),
            ], [0, 0, 0]);
            var result = projection.ProjectOnto(target, Vector3.UnitY);
            Assert.Equal(new Vector3(5, 3, 5), result.Vertices[0].Position);
        }

        [Fact]
        public void SlopedSurfaceDrapesFlatProjection() {
            //Ramp y=x from (0,0) to (2,1)
            var target = new Mesh();
            target.AddVertex(new Vertex(new(0, 0, 0), Colors.White, new(0, 0)));
            target.AddVertex(new Vertex(new(2, 1, 0), Colors.White, new(2, 0)));
            target.AddVertex(new Vertex(new(2, 1, 2), Colors.White, new(2, 2)));
            target.AddVertex(new Vertex(new(0, 0, 2), Colors.White, new(0, 2)));
            target.AddIndices([0, 1, 2, 0, 2, 3]);

            var projection = new Mesh(null, [
                new Vertex(new(1, 0.55f, 0.5f), Colors.White, new(1, 0.5f)),
                new Vertex(new(0.5f, 0.05f, 0.5f), Colors.White, new(0.5f, 0.5f)),
            ], [0, 0, 0]);
            var result = projection.ProjectOnto(target, Vector3.UnitY, float.PositiveInfinity, -10);
            Assert.True(MathF.Abs(result.Vertices[0].Position.Y - 0.5f) < 1e-4f);
            Assert.True(MathF.Abs(result.Vertices[1].Position.Y - 0.25f) < 1e-4f);
        }

        [Fact]
        public void EmptyTargetKeepsVertices() {
            var target = new Mesh();
            var projection = QuadProjection(0);
            var result = projection.ProjectOnto(target, Vector3.UnitY);
            Assert.Equal(4, result.Vertices.Count);
            Assert.Equal(projection.Vertices[0].Position, result.Vertices[0].Position);
        }

        [Fact]
        public void InvalidArgumentsThrow() {
            var target = FlatQuadTarget();
            var projection = QuadProjection(0);
            Assert.Throws<ArgumentException>(() => projection.ProjectOnto(target, Vector3.Zero));
            Assert.Throws<ArgumentException>(() => projection.ProjectOnto(target, Vector3.UnitY, 1, 2));
        }

        [Fact]
        public void RealWorldSlopedSectionDrapes() {
            //Mimics GenerateSectionMesh with two level-frame nodes at different heights (like the log: -9.8 and -8.44)
            var nodeA = new Vector3(16, -9.8f, 14);
            var nodeB = new Vector3(46, -8.44f, 14);
            var tangent = new Vector3(1, 0, 0);
            var lateral = new Vector3(0, 0, 1);
            var bounds = 10;

            //surfaceMesh: exactly what GenerateIntersectionStrip builds - crossover splines, woven, strip-drawn
            var startLeft = nodeA + lateral * -bounds;
            var startRight = nodeA + lateral * bounds;
            var endRight = nodeB + lateral * bounds;
            var endLeft = nodeB + lateral * -bounds;
            var leftEdge = GeometryUtils.GenerateSplinePoints(startLeft, endRight, tangent, tangent, 17);
            var rightEdge = GeometryUtils.GenerateSplinePoints(startRight, endLeft, tangent, tangent, 17);
            var surfaceMesh = new Mesh();
            surfaceMesh.DrawStrip(GeometryUtils.WeaveStrip(leftEdge, rightEdge).Select(GeometryUtils.CreateVertex).ToArray());

            //WorkingPlane: Center between the nodes, X = lateral, Y = tangent of node A (SectionCache convention)
            var workingPlane = new WorkingPlane((nodeA + nodeB) / 2, lateral, tangent);
            var normal = Vector3.UnitY;

            //Marking quad: flattened through Project/Unproject on the working plane (ProjectStripOntoWorkingPlane + CreateMeshingFunction roundtrip), offset +0.05
            var reach = surfaceMesh.BoundingBox().Extent();
            var marking = new Mesh();
            marking.AddVerts(new[] {
                new[] { -8f, -12f }, new[] { 8f, -12f }, new[] { 8f, 12f }, new[] { -8f, 12f },
            }.Select(c => {
                var pos = workingPlane.Unproject(new Vector2(c[0], c[1])) + normal * 0.05f;
                return new Vertex(pos, Colors.White, new(c[0], c[1]));
            }).ToArray());
            marking.AddIndices([0, 1, 2, 0, 2, 3]);

            var result = marking.ProjectOnto(surfaceMesh, normal, float.PositiveInfinity, -reach);

            //All 4 vertices must leave the flat working plane and follow the surface heights
            var projectedYs = result.Vertices.Select(v => v.Position.Y).ToList();
            Assert.All(projectedYs, y => Assert.False(MathF.Abs(y - (-9.07f)) < 1e-3f, $"vertex stayed flat at {y}"));
            //Surface spans [-9.8, -8.44]: draping must reach both extremes
            Assert.True(MathF.Abs(projectedYs.Min() - (-9.8f)) < 0.35f, $"min Y {projectedYs.Min()} did not drape to the low side");
            Assert.True(MathF.Abs(projectedYs.Max() - (-8.44f)) < 0.35f, $"max Y {projectedYs.Max()} did not drape to the high side");
        }

        [Fact]
        public void SurfaceOffsetIsReappliedAfterProjection() {
            var target = FlatQuadTarget();
            var projection = new Mesh(null, [
                new Vertex(new(0.5f, 0, 0.5f), Colors.White, new(0.5f, 0.5f)),
            ], [0, 0, 0]);
            var result = projection.ProjectOnto(target, Vector3.UnitY, float.PositiveInfinity, -10, 0.05f);
            Assert.True(MathF.Abs(result.Vertices[0].Position.Y - 0.05f) < 1e-5f);
        }

        [Fact]
        public void RimMissSnapsToClosestSurfacePoint() {
            var target = FlatQuadTarget();
            //0.5 past the x=1 rim - outside the AABB, no ray hit possible
            var projection = new Mesh(null, [
                new Vertex(new(1.5f, 0.05f, 0.5f), Colors.White, new(1.5f, 0.5f)),
            ], [0, 0, 0]);
            var result = projection.ProjectOnto(target, Vector3.UnitY, float.PositiveInfinity, -10, 0.05f);
            var p = result.Vertices[0].Position;
            Assert.True(MathF.Abs(p.X - 1) < 1e-5f, $"X {p.X} did not snap to the rim");
            Assert.True(MathF.Abs(p.Y - 0.05f) < 1e-5f, $"Y {p.Y} did not drape");
        }

        [Fact]
        public void TryProjectPointHandlesCoplanarPoint() {
            var target = FlatQuadTarget();
            var hit = target.TryProjectPoint(new Vector3(0.5f, 0, 0.5f), Vector3.UnitY, out var projected, out _, out _);
            Assert.True(hit);
            Assert.True(MathF.Abs(projected.Y) < 1e-4f);
        }

        [Fact]
        public void DiagnoseMiss() {
            var nodeA = new Vector3(16, -9.8f, 14);
            var nodeB = new Vector3(46, -8.44f, 14);
            var tangent = new Vector3(1, 0, 0);
            var lateral = new Vector3(0, 0, 1);
            var bounds = 10;

            var startLeft = nodeA + lateral * -bounds;
            var startRight = nodeA + lateral * bounds;
            var endRight = nodeB + lateral * bounds;
            var endLeft = nodeB + lateral * -bounds;
            var leftEdge = GeometryUtils.GenerateSplinePoints(startLeft, endRight, tangent, tangent, 17);
            var rightEdge = GeometryUtils.GenerateSplinePoints(startRight, endLeft, tangent, tangent, 17);
            var surfaceMesh = new Mesh();
            surfaceMesh.DrawStrip(GeometryUtils.WeaveStrip(leftEdge, rightEdge).Select(GeometryUtils.CreateVertex).ToArray());

            var box = surfaceMesh.BoundingBox();
            Console.WriteLine($"surface bounds: min={box.Min} max={box.Max} extent={box.Extent()} tris={surfaceMesh.Indices.Count / 3}");

            var reach = box.Extent();
            var vertex = new Vector3(43, -9.07f, 14);
            var dir = Vector3.UnitY;

            var ray = new Ray3(vertex - dir * (reach + 0.001f), dir);
            var bvhHit = surfaceMesh.ComputeIntersection(ray, out var dist, out _);
            Console.WriteLine($"BVH hit: {bvhHit} dist={dist} point={ray.GetPoint(dist)}");

            float best = float.MaxValue;
            for (int i = 0; i < surfaceMesh.Indices.Count - 2; i += 3) {
                var hit = GeometryUtils.RayIntersectsTriangle(ray,
                    surfaceMesh.Vertices[surfaceMesh.Indices[i]].Position,
                    surfaceMesh.Vertices[surfaceMesh.Indices[i + 1]].Position,
                    surfaceMesh.Vertices[surfaceMesh.Indices[i + 2]].Position,
                    out var d, 0, float.PositiveInfinity);
                if (hit && d < best) best = d;
            }
            Console.WriteLine($"brute force best hit: {best} point={ray.GetPoint(best)}");

            //where do the woven points actually land?
            var woven = GeometryUtils.WeaveStrip(leftEdge, rightEdge);
            Console.WriteLine($"leftEdge[16]={leftEdge[16]} rightEdge[16]={rightEdge[16]}");
            Console.WriteLine($"leftEdge[0]={leftEdge[0]} rightEdge[0]={rightEdge[0]}");
        }

        [Fact]
        public void MultiMeshProjectionPreservesBins() {
            var target = FlatQuadTarget();
            var multimesh = new MultiMesh();
            var bin = multimesh.GetOrCreateRenderBin(new SimpleMaterial(), null);
            bin.DrawModel(QuadProjection(0.05f));
            var result = multimesh.ProjectOnto(target, Vector3.UnitY, float.PositiveInfinity, -10);
            var resultBin = Assert.Single(result.RenderBins);
            Assert.Equal(new SimpleMaterial(), resultBin.Key);
            Assert.All(resultBin.Value.Vertices, v => Assert.True(MathF.Abs(v.Position.Y) < 1e-4f));
        }
    }
}
