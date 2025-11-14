using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model {
    public class RenderBinConverter {
        //OLD ==> NEW
        public MeshComplex MultiMesh2MeshComplex(MultiMesh mesh) {
            MeshComplex mc = new MeshComplex();
            foreach(var row in mesh.RenderBins) {
                SimpleMaterial mat = new SimpleMaterial();
                mat.Texture = row.Key;
                var meshElement = RenderBin2MeshElement(mat, row.Value);
                mc.AddElement(meshElement);
            }
            return mc;
        }
        public MeshElement<SimpleMaterial, VertexPositionColorTexture> RenderBin2MeshElement(SimpleMaterial material, IRenderBin mesh) {
            var tricount = mesh.Indices.Count / 3;
            MeshTri[] tris = new MeshTri[tricount];
            for (int i = 0; i < tricount; i+=3) {
                var tri = new MeshTri();
                tri.A = mesh.Indices[i];
                tri.B = mesh.Indices[i+1];
                tri.C = mesh.Indices[i+2];
                tris[i/3] = tri;
            }
            foreach (var tagrow in mesh.Tags) {
                var tri = tris[tagrow.Key];
                tri.Tag = tagrow.Value;
                tris[tagrow.Key] = tri;
            }
            return new MeshElement<SimpleMaterial, VertexPositionColorTexture>(material.Texture.Name, material, mesh.Vertices.ToArray(), tris);
        }

        //NEW ==> OLD
        public MultiMesh MeshComplex2MultiMesh(MeshComplex mc) {
            var mesh = new MultiMesh();
            foreach(var row in mc.Elements) {
                var meshElement0 = row.Value;
                if(meshElement0 is MeshElement<SimpleMaterial, VertexPositionColorTexture> meshElement) {
                    IRenderBin bin = mesh.GetOrCreateRenderBinForced(meshElement.Material.Texture);
                    MeshElement2Mesh(meshElement, bin);
                }
            }
            return mesh;
        }
        public Mesh MeshElement2Mesh(MeshElement<SimpleMaterial, VertexPositionColorTexture> meshElement) {
            Mesh mesh = new Mesh();
            MeshElement2Mesh(meshElement, mesh);
            return mesh;
        }
        public void MeshElement2Mesh(MeshElement<SimpleMaterial, VertexPositionColorTexture> meshElement, IRenderBin renderBin) {
            var verts = meshElement.Vertices;
            var tris = meshElement.Triangles;
            int[] indices = new int[tris.Length * 3];
            List<KeyValuePair<int, object?>> tags = [];
            for (int i = 0; i < tris.Length; i++) {
                var j = i * 3;
                var tri = tris[i];
                indices[j] = tri.A;
                indices[j + 1] = tri.B;
                indices[j + 2] = tri.C;
                var tag = tri.Tag;
                if (tag != null) tags.Add(new(i, tag));
            }
            renderBin.DrawModel(meshElement.Vertices, indices, tags);
        }
    }
}
