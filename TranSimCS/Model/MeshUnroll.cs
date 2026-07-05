using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.ModelOld;

namespace TranSimCS.Model {
    public static class MeshUnroll {
        /// <summary>
        /// Unrolls the mesh so all mesh instances become permanent meshes of the second mesh model
        /// </summary>
        /// <param name="src">mesh to unroll</param>
        /// <param name="dst">destination mesh</param>
        public static void Unroll(this MultiMesh src, MultiMesh dst) => Unroll(src, dst, TransformQ.Identity);
        public static void Unroll(this MultiMesh src, MultiMesh dst, TransformQ transform) {
            Transform3 tx = new Transform3(transform.ToMatrix());

            //Draw transformed meshes
            foreach(var row in src.RenderBins) {
                var tex = row.Key;
                var mesh = row.Value;
                var newBin = dst.GetOrCreateRenderBinForced(tex);
                tx.TransformOutOfPlace(mesh, newBin);
            }

            //Unroll each MeshInstance
            foreach (var meshInstance in src.meshInstances) {
                var newTransform = transform * meshInstance.PositionRotation;
                Unroll(meshInstance.Mesh, dst, newTransform);
            }
        }

        public readonly struct MeshDrawInstance {
            public readonly Mesh Mesh;
            public readonly TransformQ Transform;
            public readonly SimpleMaterial Material;
            public MeshDrawInstance(Mesh mesh, TransformQ transform, SimpleMaterial material) {
                Mesh = mesh;
                Transform = transform;
                Material = material;
            }
        }
        public static class MeshTraversal {
            public static IEnumerable<MeshDrawInstance> Traverse2(MultiMesh root) {
                // Emit renderable geometry
                foreach (var bin in root.RenderBins) {
                    yield return new MeshDrawInstance(
                        bin.Value,
                        TransformQ.Identity,
                        bin.Key
                    );
                }
            }

            public static IEnumerable<MeshDrawInstance> Traverse(MultiMesh root) {
                var active = new HashSet<MultiMesh>();

                var stack = new Stack<MeshInstance>();
                stack.Push(new MeshInstance(root, TransformQ.Identity));

                while (stack.Count > 0) {
                    var frame = stack.Pop();
                    var node = frame.Mesh;

                    // Cycle detection
                    if (active.Contains(node))
                        throw new InvalidOperationException("Cycle detected in MultiMesh graph.");

                    active.Add(node);

                    // Emit renderable geometry
                    foreach (var bin in node.RenderBins) {
                        yield return new MeshDrawInstance(
                            bin.Value,
                            frame.PositionRotation,
                            bin.Key
                        );
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
