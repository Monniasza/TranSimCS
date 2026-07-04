using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Model {
    public static class MeshCycleDetector {
        public static void ValidateNoCycles(MultiMesh root) {
            var visited = new HashSet<MultiMesh>();
            var stack = new HashSet<MultiMesh>();

            DFS(root, visited, stack);
        }

        private static void DFS(
            MultiMesh node,
            HashSet<MultiMesh> visited,
            HashSet<MultiMesh> stack) {
            if (stack.Contains(node)) {
                throw new InvalidOperationException(
                    "Cycle detected in MultiMesh graph.");
            }

            if (visited.Contains(node))
                return;

            visited.Add(node);
            stack.Add(node);

            foreach (var inst in node.meshInstances) {
                if (inst.Mesh == null)
                    continue;
                DFS(inst.Mesh, visited, stack);
            }

            stack.Remove(node);
        }
    }
}
