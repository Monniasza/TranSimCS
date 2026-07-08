using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
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
using TranSimCS.SceneGraph;
using TranSimCS.Spline;

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


        public MeshGenerator<Car> Mesh { get; }

        public Property<PositionEulerAngles> PositionProp { get; }
        public MultiMesh? BodyMesh { get; private set; }

        public Property<string?> MeshIdProp;
        public string? MeshId { get => MeshIdProp.Value; set => MeshIdProp.Value = value; }

        public Car() {
            PositionProp = new(PositionEulerAngles.Zero, "position", this);
            PositionProp.ValidateChanges += (s, e) => VectorMethods.CheckPosition(e.NewValue, "position");
            MeshIdProp = new(null, "meshId", this);
            Mesh = new(this, GenerateMesh);
            MeshIdProp.ValueChanged += MeshIdProp_ValueChanged;
        }

        private void MeshIdProp_ValueChanged(object? sender, PropertyChangedEventArgs2<string?> e) {
            BodyMesh = null;
            var key = e.NewValue;
            if (loadedMeshes.TryGetValue(key, out var bm)) {
                BodyMesh = bm;
            }
            Mesh.Invalidate();
        }

        public void Randomize() {
            MeshId = "synthetic";
        }

        private void GenerateMesh(Car car, MultiMesh mesh) {
            if (BodyMesh == null) return;

            //Generate a mesh instance instead of a full mesh
            var transformQ = PositionProp.Value.CalcReferenceQuaternion();
            var meshInstance = new MeshInstance(BodyMesh, transformQ, car, true);
            mesh.meshInstances.Add(meshInstance);
            return;
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
        public LanePosition LanePosition;

        internal void Update(GameTime time) {
            if (World == null) return;
            if(LanePosition.LaneStrip == null) {
                //The car is off-road
                var vel = Velocity;
                VectorMethods.CheckVector(vel, "vel");
                var pr = PositionProp.Value;
                VectorMethods.CheckVector(pr.Position, "pr.Position");
                if (!float.IsFinite(pr.Inclination)) throw new ArithmeticException("Invalid pitch");
                if (!float.IsFinite(pr.Tilt)) throw new ArithmeticException("Invalid roll");
                var xyz = pr.Position + vel * (float)(time.ElapsedGameTime.TotalSeconds);
                VectorMethods.CheckVector(xyz, "xyz");
                pr.Position = xyz;
                PositionProp.Value = pr;
            } else {
                //If the car has an undeterminate position, infer direction from velocity and arc-length from FindT
                if (!float.IsFinite(LanePosition.LaneArcLength)) {
                    var spline = LanePosition.LaneStrip.SplineLUT.Spline;
                    var inverseInterpolatedT = Bezier3.FindT(spline, PositionProp.Value.Position, lowerLimit: -1, upperLimit: 2);
                    var splineTangential = spline.Tangential(inverseInterpolatedT);
                    var discriminant = Vector3.Dot(splineTangential, Velocity);
                    var isReverse = discriminant < 0;
                    var forwardReverseT = LanePosition.LaneStrip.SplineLUT.ByT[inverseInterpolatedT];

                    LanePosition.LaneArcLength = isReverse ? forwardReverseT.Y : forwardReverseT.X;
                    LanePosition.IsReverse = isReverse;
                }

                //Interpolate
                LanePosition.LaneArcLength += Speed * time.GetElapsedSeconds();

                //Overflow
                var splineCache = LanePosition.LaneStrip.SplineLUT;
                while (LanePosition.LaneArcLength < 0 || LanePosition.LaneArcLength > splineCache.Length) {
                    if (LanePosition.LaneStrip == null) return;
                    
                    //ASSERT T is valid
                    if (!float.IsFinite(LanePosition.LaneArcLength)) throw new ArithmeticException("Invalid newT");

                    if (LanePosition.LaneArcLength < 0) {
                        //Passed the beginning
                        Overflow(SegmentHalf.Start);
                    } else if (LanePosition.LaneArcLength > splineCache.Length) {
                        //Passed the end
                        Overflow(SegmentHalf.End);
                    }

                    if (World == null) return;
                }

                //Put the car in the world
                var laneStrip = LanePosition.LaneStrip;
                var positionCache = laneStrip.SplineLUT;
                var positionLUT = laneStrip.IsReverse() ?
                    positionCache.ReverseLUT : positionCache.ForwardLUT;

                var xyzt = positionLUT[LanePosition.LaneArcLength];
                var xyz = xyzt.ToXYZ();
                VectorMethods.CheckVector(xyz, "xyz");
                var t = xyzt.W;
                if (!float.IsFinite(t)) throw new ArithmeticException("Invalid spline paramater ");

                var lateralSpline = laneStrip.SplineCache.Item2 - laneStrip.SplineCache.Item1;
                var lateral = lateralSpline[t];
                VectorMethods.CheckVector(lateral, "lateral");
                var tangential = positionCache.Spline.Tangential(t);
                VectorMethods.CheckVector(tangential, "tangential");
                if (LanePosition.IsReverse ^ laneStrip.IsReverse()) {
                    tangential *= -1;
                    lateral *= -1;
                }

                var newCoords = PositionEulerAngles.FromPosTangentLateral(xyz, tangential, lateral);
                
                if (!float.IsFinite(newCoords.Inclination)) throw new ArithmeticException("Invalid pitch #2");
                if (!float.IsFinite(newCoords.Tilt)) throw new ArithmeticException("Invalid roll #2");

                PositionProp.Value = newCoords;
            }
        }
        private void Overflow(SegmentHalf half) {
            if (LanePosition.LaneStrip == null) return;
            LanePosition.LaneArcLength -= LanePosition.LaneStrip.SplineLUT.Length;

            var nextLane = LanePosition.LaneStrip.GetHalf(half);
            nextLane = nextLane.OppositeEnd;
            var candidates = World.FindLaneStrips(nextLane);

            //If there are no more candidates, destroy the car
            if (candidates.Count == 0) {
                World.Cars.data.Remove(this);
                return;
            }

            //Car gets stuck when hitting a next segment
            var choice = rnd.GetRandomEntry(candidates);
            if (choice == LanePosition.LaneStrip)
                throw new Exception("Transitioned to same strip");

            var isEntryFromEnd = choice.EndLane == nextLane;
            LanePosition.LaneStrip = choice;
            LanePosition.IsReverse = isEntryFromEnd;
        }
    }
}
