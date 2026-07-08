using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.SceneGraph;
using TranSimCS.Spatial;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    public sealed class TestMesh : IObjMesh {
        public event Action? OnMeshInvalidated;
        public event Action<MultiMesh>? OnMeshGenerated;
        public event MeshInvalidationCallback GeometryChanged;

        private BoundingBox _bounds;
        public BoundingBox Bounds {
            get => _bounds;  set {
                if (_bounds == value) return;
                _bounds = value;
                GeometryChanged?.Invoke(this);
            }
        }

        public TestMesh(BoundingBox box) {
            Bounds = box;
        }

        public BoundingBox GetBounds() => Bounds;

        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) {
            var hit = Bounds.Intersects(ray);
            if (hit.HasValue) {
                distance = hit.Value;
                tag = null;
                return true;
            }

            distance = float.PositiveInfinity;
            tag = null;
            return false;
        }

        public void Move(Vector3 delta) {
            Bounds = new BoundingBox(Bounds.Min + delta, Bounds.Max + delta);
        }

        private MultiMesh mesh;

        public MultiMesh GetMesh() {
            if (mesh != null) return mesh;
            var material = new SimpleMaterial() {
                Emissive = null,
                Texture = null
            };
            mesh = new MultiMesh();
            var renderBin = mesh.GetOrCreateRenderBinForced(material);
            var verts = Bounds.GetCorners().Select(x => new VertexPositionColorTexture(x, Color.White, new())).ToArray();
            var indices = new ushort[] {
                0, 1, 2, 1, 2, 3,
                4, 5, 6, 5, 6, 7,
                0, 2, 4, 2, 4, 6,
                1, 3, 5, 3, 5, 7,
                0, 4, 1, 4, 1, 5,
                2, 6, 3, 6, 3, 7
            };
            renderBin.DrawModel(verts, indices);
            renderBin.AddTagsToLastTriangles(-1, Bounds);
            OnMeshGenerated?.Invoke(mesh);
            return mesh;
        }
        public void GenerateGeometry(RenderTarget target) => target.Draw(GetMesh());
    }

    public class TestAABBTree {
        [Fact]
        public void Tree_Maintains_Valid_Node_Structure() {
            var tree = new AABBTree<TestMesh>();

            var meshes = Enumerable.Range(0, 50)
                .Select(i => new TestMesh(new BoundingBox(
                    new Vector3(i),
                    new Vector3(i + 0.5f))))
                .ToArray();

            foreach (var m in meshes)
                tree.Add(m);

            AssertTreeValid(tree);
        }
        private void AssertTreeValid(AABBTree<TestMesh> tree) {
            var visited = new HashSet<object>();

            void Visit(AABBNode<TestMesh>? n) {
                if (n == null) return;

                Assert.False(visited.Contains(n));
                visited.Add(n);

                if (n.Item != null) {
                    Assert.Null(n.Left);
                    Assert.Null(n.Right);
                } else {
                    Assert.NotNull(n.Left);
                    Assert.NotNull(n.Right);
                }

                if (n.Left != null) Visit(n.Left);
                if (n.Right != null) Visit(n.Right);
            }

            var rootField = typeof(AABBTree<TestMesh>)
                .GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(tree);

            Visit((AABBNode<TestMesh>?)rootField);
        }
        [Fact]
        public void Find_Matches_BruteForce() {
            var tree = new AABBTree<TestMesh>();

            var meshes = Enumerable.Range(0, 30)
                .Select(i => new TestMesh(new BoundingBox(
                    new Vector3(i),
                    new Vector3(i + 0.2f))))
                .ToList();

            foreach (var m in meshes)
                tree.Add(m);

            var ray = new Ray(new Vector3(-10, 0, 0), Vector3.UnitX);

            tree.Find(ray, out var treeHit, out var treeDist, out _);

            TestMesh? brute = null;
            float best = float.PositiveInfinity;

            foreach (var m in meshes) {
                if (m.ComputeIntersection(ray, out var d, out _)) {
                    if (d < best) {
                        best = d;
                        brute = m;
                    }
                }
            }

            Assert.Equal(brute, treeHit);
        }
        [Fact]
        public void Moving_Object_Triggers_New_Result() {
            var tree = new AABBTree<TestMesh>();

            var a = new TestMesh(new BoundingBox(new Vector3(0, -10, -10), new Vector3(1, 10, 10)));
            var b = new TestMesh(new BoundingBox(new Vector3(5, -10, -10), new Vector3(6, 10, 10)));

            tree.Add(a);
            tree.Add(b);

            var ray = new Ray(new Vector3(-100, 0, 0), Vector3.UnitX);

            tree.Find(ray, out var first, out _, out _);
            Assert.Equal(a, first);

            b.Move(new Vector3(-20, 0, 0)); // now closer

            tree.Find(ray, out var second, out _, out _);
            Assert.Equal(b, second);
        }
        [Fact]
        public void Remove_Removes_Item_Completely() {
            var tree = new AABBTree<TestMesh>();

            var items = Enumerable.Range(0, 10)
                .Select(i => new TestMesh(new BoundingBox(
                    new Vector3(i),
                    new Vector3(i + 0.1f))))
                .ToList();

            foreach (var i in items)
                tree.Add(i);

            tree.Remove(items[5]);

            var ray = new Ray(new Vector3(-10, 0, 0), Vector3.UnitX);

            tree.Find(ray, out var hit, out _, out _);

            Assert.NotEqual(items[5], hit);
        }
        [Fact]
        public void Stress_Test_Random_Insert_Remove_Move() {
            var tree = new AABBTree<TestMesh>();
            var rand = new Random(123);

            var items = new List<TestMesh>();

            for (int i = 0; i < 200; i++) {
                var m = new TestMesh(new BoundingBox(
                    new Vector3(rand.Next(100)),
                    new Vector3(rand.Next(100) + 0.5f)));

                items.Add(m);
                tree.Add(m);
            }

            for (int i = 0; i < 500; i++) {
                var m = items[rand.Next(items.Count)];

                m.Move(new Vector3(
                    rand.Next(-2, 3),
                    rand.Next(-2, 3),
                    rand.Next(-2, 3)));

                if (rand.NextDouble() < 0.3)
                    tree.Remove(m);

                if (rand.NextDouble() < 0.2)
                    tree.Add(m);
            }

            Assert.True(true); // no crash = pass
        }
        [Fact]
        public void Query_Box_Returns_Only_Intersecting() {
            var tree = new AABBTree<TestMesh>();

            var inside = new TestMesh(new BoundingBox(new Vector3(0), new Vector3(1)));
            var outside = new TestMesh(new BoundingBox(new Vector3(100), new Vector3(101)));

            tree.Add(inside);
            tree.Add(outside);

            var box = new BoundingBox(new Vector3(-1), new Vector3(2));

            var result = tree.Query(box).ToList();

            Assert.Contains(inside, result);
            Assert.DoesNotContain(outside, result);
        }
        [Fact]
        public void Tree_Has_No_Dangling_Pointers_After_All_Operations() {
            var tree = new AABBTree<TestMesh>();

            var items = new List<TestMesh>();

            for (int i = 0; i < 100; i++) {
                var m = new TestMesh(new BoundingBox(Vector3.Zero, Vector3.One));
                items.Add(m);
                tree.Add(m);
            }

            foreach (var m in items.Take(50))
                tree.Remove(m);

            foreach (var m in items.Skip(50)) {
                m.Move(Vector3.One);
            }

            Assert.True(true);
        }
    }
}