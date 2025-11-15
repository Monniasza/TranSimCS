using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Model.OBJ {
    public class ObjConverter {
        public static MeshElement<SimpleMaterial, VertexPositionColorTexture> ToSingleMesh(ObjData obj) {
            var meshBuilder = new MeshBuilder<SimpleMaterial, VertexPositionColorTexture>();

            foreach (var group in obj.Groups) {
                var mat = group.Material;
                var colorvector = new Vector4(mat.Kd, mat.d);
                var color = new Color(colorvector);
                Dictionary<FaceVertex, VertexPositionColorTexture> dedupedVerts = [];
                Dictionary<FaceVertex, int> lov = [];
                foreach (var Face in group.Faces) {
                    var face = Face.Vertices;
                    if (face.Count < 3) throw new IOException("Invalid vert count for a face: " + face.Count);
                    for (int i = 0; i < face.Count; i++) {
                        var fv = face[i];
                        if (dedupedVerts.ContainsKey(fv)) continue;

                        var texcoords = (fv.UVID == 0 || obj.UV.Count == 0) ? new() : obj.UV[fv.UVID - 1];
                        var normal = (fv.NormalID == 0 || obj.Normals.Count == 0) ? new() :obj.Normals[fv.NormalID - 1];
                        var position = (fv.VertexID == 0 || obj.Positions.Count == 0) ? new() : obj.Positions[fv.VertexID - 1];
                        var vert = new VertexPositionColorTexture(position, color, texcoords);
                        dedupedVerts[fv] = vert;

                        int index = meshBuilder.AddVertex(vert);
                        lov[fv] = index;
                    }
                }
                foreach (var Face in group.Faces) {
                    var face = Face.Vertices;
                    var indices = new int[face.Count];
                    for (int i = 0; i < face.Count; i++) {
                        var fv = face[i];
                        var index = lov[fv];
                        indices[i] = index;
                    }
                    var triangleFan = MeshUtil.TriangleFan(indices);
                    var tris = MeshTri.FromArray(triangleFan);
                    meshBuilder.AddTris = tris;
                }
            }
            return meshBuilder.Create();
        }
    }
}
