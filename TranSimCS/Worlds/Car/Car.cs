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
            OnStripProp = new(null, "strip", this, Equality.ReferenceEqualComparer<LaneStrip?>());
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
            var idx = rnd.Next(meshes.Count);
            var element = meshes[idx];
            MeshId = element.Item1;
        }

        private void GenerateMesh(Car car, MultiMesh mesh) {
            if (BodyMesh == null) return;

            //Generate a mesh instance instead of a full mesh
            var transformQ = PositionProp.Value.CalcReferenceQuaternion();
            var meshInstance = new MeshInstance(BodyMesh, transformQ, car, true);
            mesh.meshInstances.Add(meshInstance);
            return;

            var refframe = PositionProp.Value.CalcReferenceFrame();
            refframe.TransformOutOfPlace(BodyMesh, mesh);
            mesh.AddTagsToAll(car);
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
        public Property<LaneStrip?> OnStripProp;
        public LaneStrip? LaneStrip { get => OnStripProp.Value; set => OnStripProp.Value = value; }

        internal void Update(GameTime t) {
            var vel = Velocity;
            VectorMethods.CheckVector(vel, "vel");
            var pr = PositionProp.Value;
            VectorMethods.CheckVector(pr.Position, "pr.Position");
            if (!float.IsFinite(pr.Inclination)) throw new ArithmeticException("Invalid pitch");
            if (!float.IsFinite(pr.Tilt)) throw new ArithmeticException("Invalid roll");
            var xyz = pr.Position + vel * (float)(t.ElapsedGameTime.TotalSeconds);
            VectorMethods.CheckVector(xyz, "xyz");
            pr.Position = xyz;
            PositionProp.Value = pr;
            if (LaneStrip?.road == null) {
                return;
            }

            var splines = LaneStrip.SplineCache;
            var lspline = splines.Item1;
            var rspline = splines.Item2;
            if (LaneStrip.IsReverse()) {
                DataUtil.Swap(ref lspline, ref rspline);
                lspline = lspline.Inverse();
                rspline = rspline.Inverse();
            }
            var spline = (lspline + rspline) / 2;
            VectorMethods.CheckSpline(spline, "spline");
            var newT = Bezier3.FindT(spline, xyz, 20, 5, -1, 2);

            //ASSERT T is valid
            if (!float.IsFinite(newT)) throw new ArithmeticException("Invalid newT");

            if (newT < 0) {
                //Passed the beginning
                Overflow(SegmentHalf.Start, t, ref xyz);
            } else if (newT > 1) {
                //Passed the end
                Overflow(SegmentHalf.End, t, ref xyz);
            }

            splines = LaneStrip.SplineCache;
            lspline = splines.Item1;
            rspline = splines.Item2;
            if (LaneStrip.IsReverse()) {
                DataUtil.Swap(ref lspline, ref rspline);
                lspline = lspline.Inverse();
                rspline = rspline.Inverse();
            }
            spline = (lspline + rspline) / 2;

            //ASSERT splines are valid
            VectorMethods.CheckSpline(spline, "spline");
            var latSpline = (rspline - lspline);
            VectorMethods.CheckSpline(latSpline, "latSpline");

            newT = Bezier3.FindT(spline, xyz);
            //ASSERT T is valid
            if (!float.IsFinite(newT)) throw new ArithmeticException("Invalid newT #2");

            var lateral = latSpline[newT];
            var tangential = spline.Tangential(newT);
            VectorMethods.CheckVector(tangential, "tangential");

            var d = Vector3.Dot(tangential, Velocity);
            if (!float.IsFinite(d)) throw new ArithmeticException("Invalid d");
            if (d < 0) {
                tangential *= -1;
                lateral *= -1;
            }

            xyz = spline[newT];
            VectorMethods.CheckVector(xyz, "xyz #2");

            var newCoords = PositionEulerAngles.FromPosTangentLateral(xyz, tangential, lateral);

            if (!float.IsFinite(newCoords.Inclination)) throw new ArithmeticException("Invalid pitch #2");
            if (!float.IsFinite(newCoords.Tilt)) throw new ArithmeticException("Invalid roll #2");

            PositionProp.Value = newCoords;

        }
        private void Overflow(SegmentHalf half, GameTime t, ref Vector3 newPos) {
            if (LaneStrip == null) return;
            var nextLane = LaneStrip.GetHalf(half);
            nextLane = nextLane.OppositeEnd;
            var nextNode = nextLane.GetNodeEnd();
            var candidates = World.FindLaneStrips(nextLane);

            //If there are no more candidates, destroy the car
            if (candidates.Count == 0) {
                World.Cars.data.Remove(this);
                return;
            }

            /* If lane allows reversals, reverse now
            if (candidates.Count == 0) {
                //End of road, reverse
                Reverse();
                var endnode = nextLane.lane;
                var latpos = endnode.MiddlePosition;
                var pp = endnode.RoadNode.PositionProp.Value;
                var rf = pp.CalcReferenceFrame();
                var mirror = rf.O + rf.X * latpos;
                var actualCoords = 2*mirror - newPos;
                newPos = actualCoords;
                return;
            }*/

            //Car gets stuck when hitting a next segment
            var choice = rnd.GetRandomEntry(candidates);
            if (choice == LaneStrip)
                throw new Exception("Transitioned to same strip");
            LaneStrip = choice;
        }
    }
}
