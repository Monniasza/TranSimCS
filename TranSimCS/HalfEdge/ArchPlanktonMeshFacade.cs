using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.HalfEdge {
    public class ArchPlanktonMeshFacade : IMeshFacade {
        private readonly PlanktonMesh _mesh;
        private readonly World _world;

        // mappings
        private readonly Dictionary<int, Entity> _vertexMap = new();
        private readonly Dictionary<int, Entity> _edgeMap = new();
        private readonly Dictionary<int, Entity> _faceMap = new();

        private readonly Dictionary<Entity, int> _reverseVertex = new();
        private readonly Dictionary<Entity, int> _reverseEdge = new();
        private readonly Dictionary<Entity, int> _reverseFace = new();

        public event Action<MeshEvent> OnChanged;

        public void CollapseEdge(Entity edge) {
            throw new NotImplementedException();
        }

        public Entity CreateFace(ReadOnlySpan<Entity> vertices) {
            throw new NotImplementedException();
        }

        public Entity CreateVertex(Vector3 position) {
            int index = _mesh.Vertices.Add(position.X, position.Y, position.Z);

            var entity = _world.Create(
                new VertexComponent { PlanktonIndex = index },
                new Position { Value = position },
                new VertexAdjacency()
            );

            _vertexMap[index] = entity;
            _reverseVertex[entity] = index;

            Emit(MeshEventType.VertexCreated, entity);

            return entity;
        }

        public (Entity A, Entity B) GetEdgeVertices(Entity edge) {
            throw new NotImplementedException();
        }

        public ReadOnlySpan<Entity> GetFaceVertices(Entity face) {
            throw new NotImplementedException();
        }

        public int GetIndex(Entity entity) {
            throw new NotImplementedException();
        }

        public Vector3 GetPosition(Entity vertex) {
            throw new NotImplementedException();
        }

        public Entity GetVertexFromIndex(int index) {
            throw new NotImplementedException();
        }

        public void Rebuild() {
            throw new NotImplementedException();
        }

        public void SetPosition(Entity vertex, Vector3 position) {
            throw new NotImplementedException();
        }

        public Entity SplitEdge(Entity edge) {
            throw new NotImplementedException();
        }
    }
}