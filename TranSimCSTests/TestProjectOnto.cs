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
        public void TryProjectPointHandlesCoplanarPoint() {
            var target = FlatQuadTarget();
            var hit = target.TryProjectPoint(new Vector3(0.5f, 0, 0.5f), Vector3.UnitY, out var projected, out _, out _);
            Assert.True(hit);
            Assert.True(MathF.Abs(projected.Y) < 1e-4f);
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
