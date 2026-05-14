using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Xna.Framework;

namespace TranSimCS.HalfEdge {
    public interface IMeshFacade {
        // --- Creation ---
        Entity CreateVertex(Vector3 position);
        Entity CreateFace(ReadOnlySpan<Entity> vertices);

        // --- Topology operations ---
        Entity SplitEdge(Entity edge);
        void CollapseEdge(Entity edge);

        // --- Queries ---
        Entity GetVertexFromIndex(int index);
        int GetIndex(Entity entity);

        ReadOnlySpan<Entity> GetFaceVertices(Entity face);
        (Entity A, Entity B) GetEdgeVertices(Entity edge);

        // --- Geometry ---
        Vector3 GetPosition(Entity vertex);
        void SetPosition(Entity vertex, Vector3 position);

        // --- Events ---
        event Action<MeshEvent> OnChanged;

        // --- Sync ---
        void Rebuild(); // force resync from Plankton
    }
}
