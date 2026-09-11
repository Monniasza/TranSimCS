using System;
using System.Diagnostics;
using System.Text.Json;
using NLog;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Setting;
using TranSimCS.Worlds;
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

            //Simulate all cars
            log.Trace($"Updating {data.Count} Cars");
            Stopwatch timer = Stopwatch.StartNew();
            foreach (var car in data) car.Update(seconds);
            timer.Stop();
            log.Trace($"Updated {data.Count} Cars in {timer.Elapsed.TotalMilliseconds} ms");

            //Clear indices
            foreach(var segment in world.RoadSegments.data) {
                foreach (var strip in segment.Lanes) {
                    strip._carsOnStrip.Clear();
                }
            }

            //Index cars
            foreach (var car in data) {
                const float lookahead = 10;
                var firstStrip = car.CurrentRoute.Route.Find(car.CurrentRoute.Position);
                var lastStrip = car.CurrentRoute.Route.Find(car.CurrentRoute.Position + lookahead);
                var routePosition = car.CurrentRoute.Position;
                if (lastStrip >= car.CurrentRoute.Route.LaneStrips.Length) lastStrip = car.CurrentRoute.Route.LaneStrips.Length - 1;
                for(int i = firstStrip; i <= lastStrip; i++) {
                    var node = car.CurrentRoute.Route.LaneStrips[i];
                    var projectedPosition = node.Project(routePosition).LaneArcLength;
                    var isReverse = node.isReverse;
                    var strip = node.road;
                    var stripPosition = isReverse ? node.Span - projectedPosition : projectedPosition;
                    //if (stripPosition < 0) stripPosition = 0;
                    //if (stripPosition > node.Span) stripPosition = node.Span;
                    CarEntry entry = new(car, stripPosition, isReverse);
                    var insertionIndex = strip.FindFirstAheadIndex(stripPosition);
                    strip._carsOnStrip.Insert(insertionIndex, entry);
                }
            }

            //Spawn cars
            if (Settings.SpawnCars) {
                var chance = Settings.CarSpawnRate * seconds;
                foreach (var node in World.Nodes.data) foreach (var lane in node.Lanes) foreach (var strip in lane.Connections) {
                    if (strip.EndLane.Lane == lane) continue; //Strip ends here, do not spawn
                                                                //Check if a strip is a dead end
                    var passable = lane.IsLanePassable();
                    if (passable) continue;
                    var decision = rnd.NextSingle() < chance;
                    if (!decision) continue;
                    var enoughRoom = strip.CarsOnStrip.Count == 0 || strip.CarsOnStrip[0].positionOnStrip >= 3;
                    if(enoughRoom) Car.LaunchCar(World, strip);
                }
            }

            //Validate the car indices
#if DEBUG
            foreach (var segment in world.RoadSegments.data) {
                foreach (var strip in segment.Lanes) {
                    for (int i = 1; i < strip._carsOnStrip.Count; i++) {
                        Debug.Assert(
                            strip._carsOnStrip[i - 1].positionOnStrip <=
                            strip._carsOnStrip[i].positionOnStrip,
                            $"CarsOnStrip not sorted: " +
                            $"{strip._carsOnStrip[i - 1].positionOnStrip} > " +
                            $"{strip._carsOnStrip[i].positionOnStrip}");
                    }
                }
            }
#endif 
        }

        public override Car ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            PositionEulerAngles? pos = null;
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
                    case "pos":
                        pos = objPosConverter.Read(ref reader0, typeof(PositionEulerAngles), options);
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
            if (pos == null) throw new JsonException($"Missing pos property for car {guid}");
            Car car = new();
            car.Guid = guid.Value;
            car.MeshId = mesh;
            car.Speed = speed;
            car.CurrentRoute = strip;
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
            routeConverter.Write(writer, value.CurrentRoute, options);

            writer.WriteEndObject();
        }
    }
}
