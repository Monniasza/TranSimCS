using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using NLog;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Mode;
using TranSimCS.Model;
using TranSimCS.Model.OBJ;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spatial;
using TranSimCS.Worlds;
using static TranSimCS.Model.MeshUnroll;
using Path = System.IO.Path;

namespace TranSimCS.Cars {
    public class Car : Obj, IObjMesh, IPosition, IDemolish {
        public static Dictionary<string, MeshDrawInstance> loadedMeshes = [];
        public static ObservableList<(string, MeshDrawInstance)> meshes = [];
        private static Random rnd = new Random();
        private static string objRoot;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        public static ObjLoader newLoader;

        //Algorithms
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
                    var mesh = ObjConverter.ToSingleMesh(objData, null);
                    var mdi = new MeshDrawInstance(mesh, TransformQ.Identity, Materials.White, mesh.Indices.Count / 3);
                    bool isEmpty = mesh.Vertices.Count == 0 || mesh.Indices.Count == 0;
                    if (isEmpty) throw new ApplicationException("Empty mesh"); //Meshes not empty
                    meshes.Add((obj, mdi));
                    loadedMeshes.Add(obj, mdi);
                    mesh.Stats(log);
                } catch (Exception e) {
                    //Failed to load
                    log.Error("Failed to load a car model " + obj);
                    log.Error(e);
                    throw;
                }
            }
        }
        public static Car LaunchCar(TSWorld world, LaneStrip strip, float speed = 25) {
            var startingLane = strip.StartLane;
            var newCarPosition = startingLane.GetRoadNode().PositionProp.Value;
            if (startingLane.End == NodeEnd.Backward) newCarPosition.Azimuth ^= 1 << 31;
            Car car = new Car();
            car.Randomize();
            if (strip != null) {
                var lanePosition = new CarStripPosition(strip, 0);
                car.CurrentRoute = lanePosition.ToRoute();
            }
            car.Speed = speed;
            world.Cars.data.Add(car);
            return car;
        }

        //Authoritative properties
        public Property<string?> MeshIdProp;
        public string? MeshId { get => MeshIdProp.Value; set => MeshIdProp.Value = value; }
        public float Speed;

        private LaneStrip[]? _insertedPositions;
        private RoutePosition _route;
        public RoutePosition CurrentRoute{
            get => _route;
            set {
                RemoveFromIndices();
                _route = value;
                const float lookahead = 10;
                var firstStrip = _route.Route.Find(_route.Position);
                var lastStrip = _route.Route.Find(_route.Position + lookahead);
                var routePosition = _route.Position;
                if (lastStrip >= _route.Route.LaneStrips.Length) lastStrip = _route.Route.LaneStrips.Length - 1;
                LaneStrip[] insertedPosition = new LaneStrip[lastStrip - firstStrip + 1];
                for (int i = firstStrip; i <= lastStrip; i++) {
                    var node = _route.Route.LaneStrips[i];
                    var projectedPosition = node.Project(routePosition).LaneArcLength;
                    var isReverse = node.isReverse;
                    var strip = node.road;
                    var stripPosition = isReverse ? node.Span - projectedPosition : projectedPosition;
                    //if (stripPosition < 0) stripPosition = 0;
                    //if (stripPosition > node.Span) stripPosition = node.Span;
                    CarEntry entry = new(this, stripPosition, isReverse);
                    var insertionIndex = strip.FindFirstAheadIndex(stripPosition);
                    strip._carsOnStrip.Insert(insertionIndex, entry);
                    insertedPosition[i - firstStrip] = strip;
                }
                _insertedPositions = insertedPosition;
            }
        }

        //Derived properties
        PositionEulerAngles IPosition.PositionData {
            get => meshInstance.Transform.ToObjPos();
            set { } //ignore set
        }
        public MeshDrawInstance meshInstance;
        internal void RemoveFromIndices() {
            //Remove the car from the old road strip
            if (_insertedPositions != null) foreach (var strip in _insertedPositions)
                strip.RemoveCar(this);
            _insertedPositions = null;
        }

        public Car() {
            MeshIdProp = new(null, "meshId", this);
            MeshIdProp.ValueChanged += MeshIdProp_ValueChanged;
        }

        private void MeshIdProp_ValueChanged(object? sender, string old, string key) {
            if (loadedMeshes.TryGetValue(key, out var bm)) {
                meshInstance.Mesh = bm.Mesh;
                meshInstance.TagCount = bm.TagCount;
                meshInstance.Material = bm.Material;
            } else throw new KeyNotFoundException($"Car model {key} not found");
            GeometryChanged?.Invoke(this);
        }

        public void Randomize() {
            MeshId = "synthetic";
        }

        internal void Update(float time) {
            const float maxSpeed = 100;
            if (float.IsNaN(Speed)) {
                log.Warn($"The car {Guid} has an invalid speed. Deleting.");
                World.Cars.data.Remove(this);
                return;
            }
            if(Speed < 0) {
                log.Warn($"The car {Guid} has a negative speed of {Speed}. Inverting the speed.");
                Speed *= -1;
            }
            if(Speed > maxSpeed) {
                log.Warn($"The car {Guid} is way too fast at {Speed}. Slowing down. ");
                Speed = maxSpeed;
            }
            if (World == null) return;
            if(CurrentRoute.Route == null) {
                log.Error($"The car {Guid} is off-road. Deleting.");
                Demolish();
                return;
            }

            //Trim dead segments
            var route = CurrentRoute;
            var trimmedRoute = route.Trim();
            if (trimmedRoute == null) {
                //Route died under the car, deleting
                log.Warn($"The car {Guid} has no valid trimmed route. Deleting.");
                Demolish();
                return;
            }
            route = trimmedRoute.Value;

            //Plan the route
            const float lookahead = 10;
            var maxDeltaPos = Speed * time;
            route = route.PlanIfNeeded(maxDeltaPos);

            //Find obstacles
            var obstacle = route.FindObstacle(maxDeltaPos + lookahead, Speed, this);

            //Interpolate
            const float minMovement = 0;
            var deltaPos = obstacle.relativeDistance;
            if (deltaPos > maxDeltaPos) deltaPos = maxDeltaPos;
            if (deltaPos < minMovement) deltaPos = minMovement;
            var newRoute = route.Advance(deltaPos);
            if (newRoute == null) {
                Demolish();
                return;
            }
            route = newRoute.Value;

            //Put the car in the world
            var referenceFrame = route.GetPositionFrame();
            var newCoords = referenceFrame.ToQuaternion();

            meshInstance.Transform = newCoords;
            GeometryChanged?.Invoke(this);
            CurrentRoute = route;
        }

        public void Demolish() {
            World.Cars.data.Remove(this);
            return;
        }

        //Geometry
        public event MeshInvalidationCallback GeometryChanged;
        public void GenerateGeometry(RenderTarget target) {
            if(meshInstance.Mesh != null)
                target.Draw(meshInstance);
        }
        public AABB GetBounds() => meshInstance.Mesh == null ? default : OBB.TransformBoundingBox(meshInstance.Mesh.GetBounds(), meshInstance.Transform);
        public bool ComputeIntersection(Ray3 ray, out float distance, out object? tag) {
            if(meshInstance.Mesh == null) return IBVHElement.Reject(ray, out distance, out tag);
            ray = meshInstance.Transform.Inverse().Transform(ray);
            var intersect = meshInstance.Mesh.ComputeIntersection(ray, out distance, out _);
            tag = intersect ? this : null;
            return intersect;
        }
    }
}
