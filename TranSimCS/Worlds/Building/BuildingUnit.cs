using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Maths;
using MonoGame.Extended.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.SceneGraph;

namespace TranSimCS.Worlds.Building {
    public class BuildingUnit : Obj, IPosition, IObjMesh<BuildingUnit> {
        public Property<ObjPos> PositionProp { get; }
        public Property<Vector3i> UnitSizeProp { get; }
        public MeshGenerator<BuildingUnit> Mesh { get; }

        public BuildingUnit() {
            PositionProp = new Property<ObjPos>(ObjPos.Zero, "pos", this);
            UnitSizeProp = new Property<Vector3i>(new(8, 2, 4), "size", this);
            Mesh = new MeshGenerator<BuildingUnit>(this, GenerateMesh);
        }

        private static void GenerateMesh(BuildingUnit unit, MultiMesh mesh) {
            var size = unit.UnitSizeProp.Value;
            var width = size.x;
            var depth = size.z;
            var height = size.y;

            //Generate the sides
            var windowsMesh = mesh.GetOrCreateRenderBinForced(Assets.BuildingWindows);
            EmitStrip(unit, unit.UnitSizeProp.Value, windowsMesh);

            //Generate the rooftop
            var roofMesh = mesh.GetOrCreateRenderBinForced(Assets.Concrete);
            var dx = Vector3.UnitX * 4 * width;
            var dy = Vector3.UnitZ * 4 * depth;
            roofMesh.DrawParallelogram(Vector3.UnitY * 4 * height, dx, dy, Color.White, new RectangleF(0, 0, width, depth));
            roofMesh.DrawParallelogram(Vector3.Zero,               dx, dy, Color.White, new RectangleF(0, 0, width, depth));
            roofMesh.AddTagsToLastTriangles(4, unit);

            //Transform the object
            var refframe = unit.PositionProp.Value.CalcReferenceFrame();
            refframe.TransformInPlace(mesh);
        }
        private static void EmitStrip(object tag, Vector3i size, IRenderBin mesh) {
            var width = size.x;
            var depth = size.z;
            var height = size.y;
            var depth4 = 4 * depth;
            var width4 = 4 * width;
            var height4 = 4 * height;

            float[] U = [0, width, depth + width, 2 * width + depth, 2 * depth + 2 * width];
            float[] V = [0, height];
            float[] X = [0, width4, width4, 0, 0];
            float[] Z = [0, 0, depth4, depth4, 0];
            float[] Y = [height4, 0];
            var verts = new VertexPositionColorTexture[10];

            int i = 0;
            for(int u = 0; u < 5; u++) {
                for(int v = 0; v < 2; v++) {
                    var texX = U[u];
                    var texY = V[v];
                    var posX = X[u];
                    var posZ = Z[u];
                    var posY = Y[v];
                    var vertex = new VertexPositionColorTexture(new(posX, posY, posZ), Color.White, new(texX, texY));
                    verts[i] = vertex;
                    i++;
                }
            }
            mesh.DrawStrip(verts);
            mesh.AddTagsToLastTriangles(8, tag);
        }
    }
}
