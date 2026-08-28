using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MLEM.Maths;
using MonoGame.Extended;
using NLog;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Model.OBJ;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2.TypeRegistry;
using TranSimCS.SceneGraph;
using TranSimCS.Spline;
using Path = System.IO.Path;

namespace TranSimCS.Worlds.Cars {
    public class Car : Obj, IObjMesh, IPosition {
        public static Dictionary<string, MultiMesh> loadedMeshes = [];
        public static ObservableList<(string, MultiMesh)> meshes = [];
        private static Random rnd = new Random();
        private static string objRoot;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        public static ObjLoader newLoader;

        public static readonly TypeRegistry<CarPosition> CarPositionRegistry;

        static Car() {
            CarPositionRegistry = new();
            CarPositionRegistry.Register(CarStripPosition.StripTypeName, new LanePositionConverter());
        }

        public static void Init() {
            //Load all meshes
            objRoot = Path.Combine(Program.DataRoot, "Files", "eracoon_cars", "obj");

            static Stream objFinder(string x) => File.OpenRead(x);
            static Stream mtlFinder(string x) => File.OpenRead(Path.Combine(objRoot, x));

            newLoader = new(null);

            var syntheticMesh = CarModel.CreateModel();
            var meshName = "synthetic";
            loadedMeshes.Add(meshName, syntheticMesh);
            meshes.Add((meshName, syntheticMesh));

            //Find all cars in the directory and load them
            var objs = Directory.GetFiles(objRoot).Where(x => x.EndsWith(".obj"));
            foreach (var obj in objs) {
                try {
                    log.Info("Loading car mesh " + obj);
                    var objData = newLoader.LoadObj(obj);
                    var multimesh = new MultiMesh();
                    var submesh = multimesh.GetOrCreateRenderBinForced(Assets.White);
                    var mesh = ObjConverter.ToSingleMesh(objData, submesh);
                    bool isEmpty = mesh.Vertices.Count == 0 || mesh.Indices.Count == 0;
                    if (isEmpty) throw new ApplicationException("Empty mesh"); //Meshes not empty
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

        public Property<PositionEulerAngles> PositionProp { get; }
        public MultiMesh? BodyMesh { get; private set; }

        public Property<string?> MeshIdProp;
        public string? MeshId { get => MeshIdProp.Value; set => MeshIdProp.Value = value; }

        public Car() {
            PositionProp = new(PositionEulerAngles.Zero, "position", this);
            PositionProp.ValidateChanges += (s, old, val) => VectorMethods.CheckPosition(val, "position");
            PositionProp.ValueChanged += PositionProp_ValueChanged;
            MeshIdProp = new(null, "meshId", this);
            MeshIdProp.ValueChanged += MeshIdProp_ValueChanged;
        }

        public TransformQ transformQ { get; private set; }
        public MeshInstance meshInstance { get; private set; }

        private void PositionProp_ValueChanged(object? sender, PositionEulerAngles old, PositionEulerAngles val) {
            transformQ = val.ToTransformQ();
            meshInstance = new(BodyMesh, transformQ, this, true);
        }

        private void MeshIdProp_ValueChanged(object? sender, string old, string key) {
            BodyMesh = null;
            if (loadedMeshes.TryGetValue(key, out var bm)) {
                BodyMesh = bm;
            }

            meshInstance = new(BodyMesh, transformQ, this, true);

            GeometryChanged?.Invoke(this);
        }

        public void Randomize() {
            MeshId = "synthetic";
        }

        public float Speed;
        public Vector3 Velocity {
            get => PositionProp.Value.GetTangential() * Speed;
            set {
                var atan3 = PositionEulerAngles.Atan3(value);
                var pr = PositionProp.Value;
                pr.Azimuth = GeometryUtils.RadiansToField(atan3.Azimuth);
                pr.Inclination = atan3.Inclination;
                Speed = value.Length();
            }
        }
        public void Reverse() {
            var pr = PositionProp.Value;
            pr.Azimuth += RoadNode.AZIMUTH_SOUTH;
            pr.Inclination *= -1;
            PositionProp.Value = pr;
        }
        public CarPosition? LanePosition;

        public event MeshInvalidationCallback GeometryChanged;

        internal void Update(float time) {
            if (World == null) return;
            if(LanePosition == null) {
                //The car is off-road
                var vel = Velocity;
                VectorMethods.CheckVector(vel, "vel");
                var pr = PositionProp.Value;
                VectorMethods.CheckVector(pr.Position, "pr.Position");
                if (!float.IsFinite(pr.Inclination)) throw new ArithmeticException("Invalid pitch");
                if (!float.IsFinite(pr.Tilt)) throw new ArithmeticException("Invalid roll");
                var xyz = pr.Position + vel * time;
                VectorMethods.CheckVector(xyz, "xyz");
                pr.Position = xyz;
                PositionProp.Value = pr;
            } else {
                //Interpolate
                LanePosition = LanePosition.Advance(Speed * time);
                if (LanePosition == null) {
                    Destroy();
                    return;
                }

                //Overflow
                var maxLength = LanePosition.MaxPosition();
                if (maxLength < 0.1) throw new ArithmeticException("Zero or negative length");
                while (LanePosition.CurrentPosition() < 0 || LanePosition.CurrentPosition() > maxLength) {
                    if (LanePosition == null) return;

                    var overflowIsRear = LanePosition.CurrentPosition() < 0;

                    if (overflowIsRear) {
                        //Passed the beginning
                        Overflow(SegmentHalf.Start);
                    } else {
                        //Passed the end
                        Overflow(SegmentHalf.End);
                    }

                    if (World == null) return;
                }

                //Put the car in the world
                var positionLUT = LanePosition.GetPositionLookup();

                var xyzt = positionLUT[LanePosition.CurrentPosition()];
                var xyz = xyzt.ToXYZ();
                VectorMethods.CheckVector(xyz, "xyz");
                var t = xyzt.W;
                if (!float.IsFinite(t)) throw new ArithmeticException("Invalid spline paramater ");

                var referenceFrame = LanePosition.GetPositionFrame(t);
                var lateral = referenceFrame.X;
                VectorMethods.CheckVector(lateral, "lateral");
                var tangential = referenceFrame.Z;
                VectorMethods.CheckVector(tangential, "tangential");

                xyz = referenceFrame.O;

                var newCoords = PositionEulerAngles.FromPosTangentLateral(xyz, tangential, lateral);
                
                if (!float.IsFinite(newCoords.Inclination)) throw new ArithmeticException("Invalid pitch #2");
                if (!float.IsFinite(newCoords.Tilt)) throw new ArithmeticException("Invalid roll #2");

                PositionProp.Value = newCoords;
            }

            meshInstance = new(BodyMesh, PositionProp.Value.ToTransformQ(), this);
        }
        private void Overflow(SegmentHalf half) {
            if (LanePosition == null) return;
            var nextPosition = LanePosition.CurrentPosition() - LanePosition.MaxPosition();

            var candidates = LanePosition.FindNext(half).ToArray();

            //If there are no more candidates, destroy the car
            if (candidates.Length == 0) {
                Destroy();
                return;
            }

            //Car gets stuck when hitting a next segment
            var choice = rnd.GetRandomEntry(candidates);
            if (choice == LanePosition)
                throw new Exception("Transitioned to same strip");

            LanePosition = choice.Advance(nextPosition);
        }

        public void Destroy() {
            World.Cars.data.Remove(this);
            LanePosition = null;
            return;
        }

        public void GenerateGeometry(RenderTarget target) => target.Draw(meshInstance);
        public AABB GetBounds() => meshInstance.GetBounds();
        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) => meshInstance.ComputeIntersection(ray, out distance, out tag);
    }
}
