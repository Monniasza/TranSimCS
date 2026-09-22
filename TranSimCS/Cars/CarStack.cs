using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using NLog;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Setting;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;
using TranSimCS.Worlds.Stack;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TranSimCS.Cars {
    public class CarStack : ObjectStack<Car, CarStack> {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private static readonly Random rnd = new Random();

        private readonly TSWorld world;
        public readonly TrackerSpatial<Car, CarStack> trackerSpatial;
        public CarStack(TSWorld world) : base(world) {
            this.world = world;
            trackerSpatial = new TrackerSpatial<Car, CarStack>(world);
            stackTrackers.Add(trackerSpatial);

            //Add a car every 5 seconds on each lane
            world.OnUpdate += World_OnUpdate;
        }

        private void World_OnUpdate(float seconds) {
            //Clear car indices
            foreach (var path in world.Paths.Paths.Values)
                path._carsOnStrip.Clear();

            //Generate all car indices
            List<SplinePath> insertedLaneStrips = [];
            void InsertCarIntoStrip(CarEntry car, SplinePath strip) {
                if(strip._carsOnStrip.Count == 0) insertedLaneStrips.Add(strip);
                strip._carsOnStrip.Add(car);
            }

            foreach(var car in data) {
                //Trim route elements whose lane strip died (deleted/reversed) before indexing them
                car.TrimUntilDead();
                if (car.RouteElementCount == 0) continue;

                var stripIndex = car.FindIndexFromDistance(car.RoutePositionFromStart);
                var routePosition = car.RoutePositionFromStart;
                for (int i = 0; i < stripIndex; i++) routePosition -= car.GetRouteElement(i).road.GetSpline().Length;

                var node = car.GetRouteElement(stripIndex);
                var isReverse = node.isReverse;
                var strip = node.road;
                var stripPosition = isReverse ? strip.GetSpline().Length - routePosition : routePosition;
                CarEntry entry = new(car, stripPosition, isReverse);
                InsertCarIntoStrip(entry, strip);
                routePosition -= node.road.GetSpline().Length;
            }

            //Sort car lists
            foreach (var strip in insertedLaneStrips) strip._carsOnStrip.Sort();

            //Simulate all cars
            log.Trace($"Updating {data.Count} Cars");
            Stopwatch timer = Stopwatch.StartNew();
            foreach (var car in data) car.Update(seconds);
            timer.Stop();
            log.Trace($"Updated {data.Count} Cars in {timer.Elapsed.TotalMilliseconds} ms");

            //Spawn cars
            if (Settings.SpawnCars) {
                var chance = Settings.CarSpawnRate * seconds;
                foreach (var node in World.Nodes.data) foreach (var lane in node.Lanes) foreach (var strip in lane.Connections) {
                    if (strip.EndLane.Lane == lane) continue; //Strip ends here, do not spawn
                                                              //Check if a strip is a dead end
                    var path = strip.Path;
                    var passable = lane.IsLanePassable();
                    if (passable) continue;
                    var decision = rnd.NextSingle() < chance;
                    if (!decision) continue;
                    var enoughRoom = path.CarsOnStrip.Count == 0 || path.CarsOnStrip[0].positionOnStrip >= 5;
                    if(enoughRoom) Car.LaunchCar(World, strip);
                }
            }

            //Validate the car indices
#if DEBUG
            foreach (var path in world.Paths.Paths.Values) {
                for (int i = 1; i < path._carsOnStrip.Count; i++) {
                    Debug.Assert(
                        path._carsOnStrip[i - 1].positionOnStrip <=
                        path._carsOnStrip[i].positionOnStrip,
                        $"CarsOnStrip not sorted: " +
                        $"{path._carsOnStrip[i - 1].positionOnStrip} > " +
                        $"{path._carsOnStrip[i].positionOnStrip}");
                }
            }
            
#endif 
        }

        public override Car ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            string? mesh = null;
            float speed = 0;
            RoutePosition strip = default;

            var objPosConverter = new ObjPosConverter();
            var stripConverter = new LanePositionConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "id":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "mesh":
                        reader0.Read();
                        mesh = reader0.GetString();
                        break;
                    case "speed":
                        reader0.Read();
                        speed = reader0.GetSingle();
                        break;
                    case "strip":
                        strip = stripConverter.Read(ref reader0, typeof(LaneStrip), options).Value.ToRoute();
                        break;
                    case "state":
                        JsonProcessor.ReadJsonObjectProperties(ref reader0, (ref reader1, innerName) => {
                            if (innerName == "data") {
                                strip = stripConverter.Read(ref reader1, typeof(LaneStrip), options).Value.ToRoute();
                            } else reader1.Skip();
                        });
                        break;
                    case "route":
                        var routeConverter = new RoutePositionConverter(world);
                        strip = routeConverter.Read(ref reader0, typeof(RoutePosition), options);
                        break;
                    default:
                        reader0.Skip(); break;
                }
            });

            if (guid == null) throw new JsonException("Missing id property");
            Car car = new();
            car.Guid = guid.Value;
            car.MeshId = mesh;
            car.Speed = speed;
            car.SetRoute(strip);
            return car;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, Car value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString("id", value.Guid.ToString());

            writer.WritePropertyName("mesh");
            writer.WriteStringValue(value.MeshId);

            writer.WritePropertyName("speed");
            writer.WriteNumberValue(value.Speed);

            writer.WritePropertyName("route");
            var routeConverter = new RoutePositionConverter(world);
            routeConverter.Write(writer, value.GetRoute(), options);

            writer.WriteEndObject();
        }
    }
}
