using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NLog;
using ObjLoader.Loader.Data;
using ObjLoader.Loader.Data.Elements;
using ObjLoader.Loader.Loaders;

namespace TranSimCS.Model {
    public class OBJConverter {

        private readonly Func<string, Texture2D> texFinder;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public OBJConverter(Func<string, Texture2D> texFinder) {
            this.texFinder = texFinder;

        }

        public Mesh ConvertToSingleMesh(LoadResult result) {

            var mesh = new Mesh();

            var convertedTexCoords = result.Textures.Select(x => new Vector2(x.X, x.Y)).ToArray();
            var convertedPositions = result.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();

            log.Info($"texcoords.Count = {convertedTexCoords.Length}, positions.Count = {convertedPositions.Length}");
            
            foreach(var group in result.Groups) {
                List<VertexPositionColorTexture> groupVerts = new List<VertexPositionColorTexture>();
                var mat = group.Material;
                var colorvector = new Vector4(mat.DiffuseColor.X, mat.DiffuseColor.Y, mat.DiffuseColor.Z, 1 - mat.Transparency);
                var color = new Color(colorvector);

                Dictionary<long, VertexPositionColorTexture> dedupedVerts = [];
                Dictionary<long, int> lov = [];
                foreach (var face in group.Faces) {
                    if (face.Count != 3) throw new IOException("Invalid vert count for a face: " + face.Count);
                    for (int i = 0; i < face.Count; i++) {
                        var fv = face[i];
                        var k = Key(fv);
                        if (dedupedVerts.ContainsKey(k)) continue;

                        var texcoords = (convertedTexCoords.Length == 0) ? new() : convertedTexCoords[fv.TextureIndex];
                        var position = convertedPositions[fv.VertexIndex - 1];
                        var vert = new VertexPositionColorTexture(position, color, texcoords);
                        dedupedVerts[k] = vert;

                        int index = mesh.AddVertex(vert);
                        lov[k] = index;
                    }
                }

                foreach (var face in group.Faces) {
                    for (int i = 0; i < face.Count; i++) {
                        var fv = face[i];
                        var k = Key(fv);
                        var index = lov[k];
                        mesh.AddIndex(index);
                    }
                }
            }
            return mesh;
        }

        public long Key(FaceVertex fv) {
            return ((long)fv.VertexIndex) << 32 | ((long)fv.TextureIndex);
        }

        /*
        public MultiMesh TransformToStandardMesh(LoadResult result, Func<string, string> replacer) {
            if (replacer == null) replacer = x => x;
            var mesh = new MultiMesh();
            var verts = new VertexPositionColorTexture[result.Vertices.Count];
            for (int i = 0; i < result.Vertices.Count; i++) {
                var nvert = result.Vertices[i];
                verts[i].Position = new(nvert.X, nvert.Y, nvert.Z);
            }
            Dictionary<string, Material> mats = [];
            for (int i = 0; i < result.Materials.Count; i++) {
                var mat = result.Materials[i];
                mats[mat.Name] = mat;
            }
            for (int i = 0; i < result.Groups.Count; i++) {
                List<VertexPositionColorTexture> groupVerts = [];
                var group = result.Groups[i];
                var mat = group.Material;
                var colorvector = new Vector4(mat.DiffuseColor.X, mat.DiffuseColor.Y, mat.DiffuseColor.Z, 1 - mat.Transparency);
                var color = new Color(colorvector);

                var texname = replacer(mat.DiffuseTextureMap);
                var tex = texFinder(texname);

                var actualMesh = mesh.GetOrCreateRenderBinForced(tex);
                //1st pass. Find unique vertices objects and assign indices in the target mesh
                Dictionary<FaceVertex, int> fvs = [];
                foreach (var face in group.Faces) {
                    for (int k = 0; k < face.Count; k++) {
                        var fv = face[k];
                        if (fvs.ContainsKey(fv)) continue; //The FaceVertex already has an index

                        //Create a new index

                    }
                }

                //Assign FaceVerti
                var texture = replacer()
            }
        }*/
    }
}
