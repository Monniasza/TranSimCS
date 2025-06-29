using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Worlds {
    /// <summary>
    /// An object placed in the world
    /// </summary>
    public abstract class Obj {
        //PROPERTIES
        public Guid Guid { get; init; } = Guid.NewGuid();
        public World World { get; private set; }

        public Obj(World world) {
            this.World = world;
            
        }

        //MESHING
        private Mesh mesh;
        public Mesh GetMesh() {
            if (mesh == null) {
                mesh = new Mesh();
                GenerateMesh(mesh);
            }
            return mesh;
        }
        public void InvalidateMesh() {
            mesh = null;
        }

        //ABSTRACT METHODS
        protected abstract void GenerateMesh(Mesh mesh);
    }
}
