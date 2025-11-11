using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Maths;
using NLog;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Model.OBJ;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Worlds.Car {
    public class Car : Obj, IObjMesh<Car>, IPosition {
        public static Dictionary<string, MultiMesh> loadedMeshes = [];
        public static ObservableList<(string, MultiMesh)> meshes = [];
        private static Random rnd = new Random();
        private static string objRoot;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public static ObjLoader newLoader;

        public static void Init() {
            //Load all meshes
            objRoot = Path.Combine(Program.DataRoot, "Files", "eracoon_cars", "obj");

            static Stream objFinder(string x) => File.OpenRead(x);
            static Stream mtlFinder(string x) => File.OpenRead(Path.Combine(objRoot, x));

            newLoader = new(null);

            //Find all cars in the directory and load them
            var objs = Directory.GetFiles(objRoot).Where(x => x.EndsWith(".obj"));

            foreach (var obj in objs) {
                try {
                    log.Info("Loading car mesh " + obj);
                    var objData = newLoader.LoadObj(obj);
                    var multimesh = new MultiMesh();
                    var submesh = multimesh.GetOrCreateRenderBinForced(Assets.White);
                    var mesh = ObjConverter.ToSingleMesh(objData, submesh);
                    meshes.Add((obj, multimesh));
                    loadedMeshes.Add(obj, multimesh);
                    mesh.Stats(log);
                } catch (Exception e) {
                    //Failed to load
                    log.Error("Failed to load a car model " + obj);
                    log.Error(e);
                    throw;
                }
                
            }
        }

        public MeshGenerator<Car> Mesh { get; }

        public Property<ObjPos> PositionProp { get; }
        public MultiMesh? BodyMesh { get; private set; }

        public Property<string?> MeshIdProp;
        public string? MeshId { get => MeshIdProp.Value; set => MeshIdProp.Value = value; }

        public readonly TSWorld World;

        public Car(TSWorld world) {
            PositionProp = new(ObjPos.Zero, "position", this);
            MeshIdProp = new(null, "meshId", this);
            Mesh = new(this, GenerateMesh);
            MeshIdProp.ValueChanged += MeshIdProp_ValueChanged;
            OnStripProp = new(null, "strip", this, Equality.ReferenceEqualComparer<LaneStrip?>());
            World = world;
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
            mesh.AddTagsToAll(car);
        }

        public Vector3 Velocity;
        public Property<LaneStrip?> OnStripProp;
        public LaneStrip? LaneStrip { get => OnStripProp.Value; set => OnStripProp.Value = value; }

        internal void Update(GameTime t) {
            var rf = PositionProp.Value.CalcReferenceFrame();
            var xyz = rf.O;
            var newXYZ = xyz + Velocity * (float)(t.ElapsedGameTime.TotalSeconds);
            if (LaneStrip == null) {
                var pr = PositionProp.Value;
                pr.Position = newXYZ;
                PositionProp.Value = pr;
                return;
            }
            var splineframe = LaneStrip.road.SplineFrame;
            var derivedCoords = splineframe.UnTransform(newXYZ, -1, 2);
            var newT = derivedCoords.Z;
            if (newT < 0) {
                //Passed the beginning
                Overflow(SegmentHalf.Start, t, ref newXYZ);
            } else if (newT > 1) {
                //Passed the end
                Overflow(SegmentHalf.End, t, ref newXYZ);
            }

            splineframe = LaneStrip.road.SplineFrame;
            derivedCoords = splineframe.UnTransform(newXYZ);
            newT = derivedCoords.Z;

            var stripSplines = RoadRenderer.GenerateSplines(LaneStrip.Tag);
            var stripSpline = (stripSplines.Item1 + stripSplines.Item2) / 2;
            var latSpline = (stripSplines.Item2 - stripSplines.Item1);

            var lateral = latSpline[newT];
            var tangential = stripSpline.Tangential(newT);

            var d = Vector3.Dot(tangential, Velocity);
            if(d < 0) {
                tangential *= -1;
                lateral *= -1;
            }

            newXYZ = stripSpline[newT];
            var newCoords = ObjPos.FromPosTangentLateral(newXYZ, tangential, lateral);
            PositionProp.Value = newCoords;

        }
        private void Overflow(SegmentHalf half, GameTime t, ref Vector3 newPos) {
            if (LaneStrip == null) return;
            var nextLane = LaneStrip.GetHalf(half);
            nextLane = nextLane.OppositeEnd;
            var nextNode = nextLane.GetNodeEnd();
            var candidates = World.FindLaneStrips(nextLane);
            if (candidates.Count == 0) {
                //End of road, reverse
                Velocity *= -1;
                var endnode = nextLane.lane;
                var latpos = endnode.MiddlePosition;
                var pp = endnode.RoadNode.PositionProp.Value;
                var mirror = pp.Position;
                var actualCoords = 2*mirror - newPos;
                newPos = actualCoords;
                return;
            }
            var choice = rnd.GetRandomEntry(candidates);
            LaneStrip = choice;
        }
    }
}
