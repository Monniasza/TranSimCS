using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using ObjLoader.Loader.Loaders;
using TranSimCS.Collections;
using TranSimCS.Model;
using TranSimCS.Model.OBJ;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds.Property;
using static TranSimCS.Model.MeshLoader;

namespace TranSimCS.Worlds.Car {
    public class Car : Obj, IObjMesh<Car>, IPosition {
        public static OBJConverter converter;
        public static MeshLoader loader;
        public static Dictionary<string, MultiMesh> loadedMeshes = [];
        public static ObservableList<(string, MultiMesh)> meshes = [];
        private static Random rnd = new Random();
        private static string objRoot;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static Model.OBJ.ObjLoader newLoader;

        public static void Init() {
            //Load all meshes
            objRoot = Path.Combine(Program.DataRoot, "Files", "eracoon_cars", "obj");

            static Stream objFinder(string x) => File.OpenRead(x);
            static Stream mtlFinder(string x) => File.OpenRead(Path.Combine(objRoot, x));
            var mspObj = new MaterialStreamAdapter(objFinder);
            var mspMtl = new MaterialStreamAdapter(mtlFinder);

            loader = new MeshLoader(objFinder, mtlFinder);
            converter = new OBJConverter(x => Assets.White);

            newLoader = new(null);

            //Find all cars in the directory and load them
            var objs = Directory.GetFiles(objRoot).Where(x => x.EndsWith(".obj"));

            foreach (var obj in objs) {
                try {
                    NewLoad(obj);
                } catch (Exception e) {
                    //Failed to load
                    log.Error("Failed to load a car model " + obj);
                    log.Error(e);
                    throw;
                }
                
            }
        }

        private static void NewLoad(string obj) {
            log.Info("Loading car mesh " + obj);
            var objData = newLoader.LoadObj(obj);
            var multimesh = new MultiMesh();
            var submesh = multimesh.GetOrCreateRenderBinForced(Assets.White);
            var mesh = ObjConverter.ToSingleMesh(objData, submesh);
            meshes.Add((obj, multimesh));
            loadedMeshes.Add(obj, multimesh);
            mesh.Stats(log);
        }

        private static void OldLoad(string obj) {
            try {
                log.Info("Loading car mesh " + obj);
                var objData = loader.Load(obj);
                var mesh = converter.ConvertToSingleMesh(objData);
                var multimesh = new MultiMesh();
                IRenderBin submesh = multimesh.GetOrCreateRenderBinForced(Assets.White);
                submesh.DrawModel(mesh);
                meshes.Add((obj, multimesh));
                loadedMeshes.Add(obj, multimesh);
                mesh.Stats(log);
            } catch (Exception e) {
                //Failed to load
                log.Error("Failed to load a car model " + obj);
                log.Error(e);
            }
        }

        public MeshGenerator<Car> Mesh { get; }

        public Property<ObjPos> PositionProp { get; }
        public MultiMesh? BodyMesh { get; private set; }

        public Property<string?> MeshIdProp;
        public string? MeshId { get => MeshIdProp.Value; set => MeshIdProp.Value = value; }

        public Car(TSWorld world) {
            PositionProp = new(ObjPos.Zero, "position", this);
            MeshIdProp = new(null, "meshId", this);
            Mesh = new(this, GenerateMesh);
            MeshIdProp.ValueChanged += MeshIdProp_ValueChanged;
        }

        private void MeshIdProp_ValueChanged(object? sender, PropertyChangedEventArgs2<string?> e) {
            BodyMesh = null;
            var key = e.NewValue;
            if(loadedMeshes.TryGetValue(key, out var bm)) {
                BodyMesh = bm;
            }
            Mesh.Invalidate();
        }

        public void Randomize() {
            var idx = rnd.Next(meshes.Count);
            var element = meshes[idx];
            MeshId = element.Item1;
        }

        private void GenerateMesh(Car car, MultiMesh mesh) {
            if (BodyMesh == null) return;
            var refframe = PositionProp.Value.CalcReferenceFrame();
            refframe.TransformOutOfPlace(BodyMesh, mesh);
        }
    }
}
