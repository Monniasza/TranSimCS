using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.ModelOld;
using TranSimCS.Spatial;
using TranSimCS.Worlds;

namespace TranSimCS.Model {
    public static class MeshUnroll {
        public struct MeshDrawInstance: IBVHElement {
            public Mesh Mesh;
            public TransformQ Transform;
            public SimpleMaterial Material;
            public int TagCount;
            public MeshDrawInstance(Mesh mesh, TransformQ transform, SimpleMaterial material, int tagCount) {
                Mesh = mesh;
                Transform = transform;
                Material = material;
                TagCount = tagCount;
            }

            public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) {
                var inverse = Transform.Inverse();
                var inverseRay = inverse.Transform(ray);
                var isIntersecting = Mesh.ComputeIntersection(inverseRay, out distance, out tag);
                return isIntersecting;
            }
            public AABB GetBounds() => OBB.TransformBoundingBox(Mesh?.GetBounds() ?? default, Transform);
        }
        public static class MeshTraversal {
            public static void Traverse(MultiMesh root, RenderTarget target, TransformQ? transform = null) {
                var active = new HashSet<MultiMesh>();

                var stack = new Stack<MeshInstance>();
                stack.Push(new MeshInstance(root, transform ?? TransformQ.Identity));

                while (stack.Count > 0) {
                    var frame = stack.Pop();
                    var node = frame.Mesh;

                    // Cycle detection
                    if (active.Contains(node))
                        throw new InvalidOperationException("Cycle detected in MultiMesh graph.");

                    active.Add(node);

                    // Emit renderable geometry
                    foreach (var bin in node.RenderBins) {
                        int tagcount = (frame.CoverTag == null) ? bin.Value.Tags.Count : bin.Value.Indices.Count / 3;
                        target(new MeshDrawInstance(
                            bin.Value,
                            frame.PositionRotation,
                            bin.Key,
                            tagcount
                        ));
                    }

                    // Push children (mesh instances)
                    var instances = node.meshInstances;

                    for (int i = instances.Count - 1; i >= 0; i--) {
                        var inst = instances[i];
                        if (inst.Mesh == null) continue;
                        inst.PositionRotation = frame.PositionRotation * inst.PositionRotation;
                        stack.Push(inst);
                    }

                    active.Remove(node);
                }
            }
        }
    }
}
