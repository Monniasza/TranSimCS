using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Worlds.Car {
    public class CarStack : ObjectStack<Car, CarStack> {
        private static readonly Random rnd = new Random();

        private readonly TSWorld world;
        public readonly TrackerSpatial<Car, CarStack> trackerSpatial;
        public readonly UpdateLoopTracker<Car, CarStack> trackerUpdate;
        public CarStack(TSWorld world) : base(world) {
            this.world = world;
            trackerSpatial = new TrackerSpatial<Car, CarStack>(world);
            trackerUpdate = new((x, t) => x.Update(t));
            stackTrackers.Add(trackerSpatial);
            stackTrackers.Add(trackerUpdate);

            //Add a car every 5 seconds on each lane
            world.OnUpdate += World_OnUpdate;
        }

        private void World_OnUpdate(Microsoft.Xna.Framework.GameTime obj) {
            if (!Settings.SpawnCars) return;

            var seconds = obj.ElapsedGameTime.TotalSeconds;
            var chance = 0.2f * seconds;

            foreach(var node in World.Nodes.data) foreach(var lane in node.Lanes) foreach(var strip in lane.Connections) {
                if (strip.EndLane.lane == lane) continue; //Strip ends here, do not spawn
                //Check if a strip is a dead end
                var passable = lane.IsLanePassable();
                if (passable) continue;
                var decision = rnd.NextSingle() < chance;
                if (decision) CarLauncherTool.LaunchCar(World, strip);
            }
        }

        public override Car ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            PositionEulerAngles? pos = null;
            string? mesh = null;
            float speed = 0;
            LaneStrip? strip = null;

            var objPosConverter = new ObjPosConverter();
            var stripConverter = new StripRefConverter(world);

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
                        strip = stripConverter.Read(ref reader0, typeof(LaneStrip), options);
                        break;
                }
            });

            if (guid == null) throw new JsonException("Missing id property");
            if (pos == null) throw new JsonException($"Missing pos property for car {guid}");
            Car car = new();
            car.Guid = guid.Value;
            car.PositionProp.Value = pos.Value;
            car.MeshId = mesh;
            car.Speed = speed;
            car.LaneStrip = strip;
            return car;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, Car value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString("id", value.Guid.ToString());

            writer.WritePropertyName("pos");
            var objPosConverter = new ObjPosConverter();
            objPosConverter.Write(writer, value.PositionProp.Value, options);

            writer.WritePropertyName("mesh");
            writer.WriteStringValue(value.MeshId);

            writer.WritePropertyName("speed");
            writer.WriteNumberValue(value.Speed);

            writer.WritePropertyName("strip");
            var stripConverter = new StripRefConverter(world);
            stripConverter.Write(writer, value.LaneStrip, options);

            writer.WriteEndObject();
        }
    }
}
