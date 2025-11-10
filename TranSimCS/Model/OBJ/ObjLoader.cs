using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using NLog;

namespace TranSimCS.Model.OBJ {
    public class ObjLoader {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static Func<string, string> GenerateLocalOpener(string dir) {
            return (x) => {
                if (Path.IsPathRooted(dir)) return File.ReadAllText(x);
                return File.ReadAllText(Path.Combine(dir, x));
            };
        }
        public string MTLSibling(string sibling, string name) {
            name = name.EndsWith(".mtl") ? name : name + ".mtl";
            try {
                return fileOpener(name);
            } catch (FileNotFoundException) {
                var root = RemoveTopLevel(sibling);
                var newpath = Path.Combine(root, name);
                return newpath;
            }
        }
        public static string RemoveTopLevel(string path) {
            var normalized = path.Replace('\\', '/');
            var split = normalized.Split('/');
            var newlist = split.SkipLast(1);
            return string.Join("/", newlist);
        }


        public readonly Func<string, string> fileOpener;
        public readonly Func<string, string, string> mtlPathConverter;

        private readonly Dictionary<string, ObjData> objs = [];
        private readonly Dictionary<string, MTLLibrary> mtllibs = [];

        public ObjLoader(Func<string, string>? fileOpener = null, Func<string, string, string>? pathConv = null) {
            this.fileOpener = fileOpener ?? File.ReadAllText;
            this.mtlPathConverter = pathConv ?? MTLSibling;
        }

        public ObjData LoadObj(string filename) {
            var path = filename.EndsWith(".obj") ? filename : filename + ".obj";
            if (objs.TryGetValue(path, out ObjData objData)) return objData;
            var data = fileOpener(path);

            List<Vector3> verts = [];
            List<Vector2> uvs = [];
            List<Vector3> norms = [];
            List<Submodel> groups = [];

            MTLLibrary currLibrary = default;
            MTL mtl = default;

            Submodel currSubmodel = new();

            foreach(var line in data.Split('\n')){
                var row = line.Trim().Split(' ');
                var kw = row[0];
                switch (kw) {
                    case "":
                    case " ":
                    case "#":
                        //Comments/empty
                        continue;
                    case "v":
                        verts.Add(ParseThree(row, 1));
                        break;
                    case "vn":
                        norms.Add(ParseThree(row, 1));
                        break;
                    case "g":
                        var newName = row[1];
                        //New submesh
                        if(currSubmodel.Faces.Count > 0) {
                            groups.Add(currSubmodel);
                            currSubmodel = new(newName);
                        }
                        break;
                    case "mtllib":
                        currLibrary = LoadMTL(filename, row[1]);
                        break;
                    case "usemtl":
                        currSubmodel.Material = currLibrary.Data[row[1]];
                        break;
                    case "f":
                        //New face
                        var elements = row.Skip(1).Select(int.Parse).Select(x => new FaceVertex(x, x, x));
                        Face f = new Face(elements.ToList());
                        currSubmodel.Faces.Add(f);
                        break;
                }
            }


            return new ObjData(verts, norms, uvs, groups);
        }

        public MTLLibrary LoadMTL(string objName, string filename) {
            

            var path = filename.EndsWith(".mtl") ? filename : filename + ".mtl";

            if(mtllibs.TryGetValue(path, out var mtllib)) return mtllib;

            path = mtlPathConverter(objName, filename);
            logger.Info($"Loading MTL: parent: {objName}, MTL name: {filename} at {path}");
            var mtlData = fileOpener(path);

            List<MTL> loadedMTLs = [];

            MTL mtl = default;

            foreach (var line in mtlData.Split('\n')) {
                var row = line.Trim().Split(' ');
                var kw = row[0];
                switch (kw) {
                    case "":
                    case " ":
                    case "#":
                        //Comments/empty
                        continue;
                    case "newmtl":
                        var mtlName = row[1];
                        if (mtl.Name != null) {
                            loadedMTLs.Add(mtl);
                        }
                        mtl = default;
                        mtl.Name = mtlName;
                        logger.Info($"MTL name: {mtlName}");
                        break;
                    case "Ns":
                        mtl.Ns = float.Parse(row[1]); break;
                    case "Ni":
                        mtl.Ni = float.Parse(row[1]); break;
                    case "Ka":
                        mtl.Ka = ParseThree(row, 1); break;
                    case "Kd":
                        mtl.Kd = ParseThree(row, 1); break;
                    case "Ks":
                        mtl.Ks = ParseThree(row, 1); break;
                    case "Ke":
                        mtl.Ke = ParseThree(row, 1); break;
                    case "d":
                        mtl.d = float.Parse(row[1]); break;
                    case "illum":
                        mtl.illum = int.Parse(row[1]); break;
                    case "map_Ka":
                    case "map_Kd":
                    case "map_Ks":
                    case "map_Ns":
                    case "disp":
                    case "decal":
                    case "bump":
                        //Not yet supported
                        break;
                    default:
                        throw new IOException("Unknown command: " + kw);
                }
            }
            if (mtl.Name != null) {
                logger.Info($"MTL name: {mtl.Name}");
                loadedMTLs.Add(mtl);
            }
            return new MTLLibrary(loadedMTLs);
        }

        public static Vector3 ParseThree(string[] data, int start) {
            float X = float.Parse(data[start]);
            float Y = float.Parse(data[start + 1]);
            float Z = float.Parse(data[start + 2]);
            return new Vector3(X, Y, Z);
        }
        public static Vector3 ParseTwo(string[] data, int start) {
            float X = float.Parse(data[start]);
            float Y = float.Parse(data[start + 1]);
            float Z = float.Parse(data[start + 2]);
            return new Vector3(X, Y, Z);
        }
    }
}
