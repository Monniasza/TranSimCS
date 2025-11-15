using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Gizmo {
    public abstract class Gizmo: Obj, IObjMesh<Gizmo> {
        public readonly Property<ObjPos> PositionProp;
        public MeshGenerator<Gizmo> Mesh { get; }

        public Gizmo() {
            PositionProp = new(ObjPos.Zero, "pos", this);
            Mesh = new MeshGenerator<Gizmo>(this, (gizmo, mesh) => GenerateMesh(mesh));
        }

        public abstract void GenerateMesh(MeshComplex mesh);
    }
}
