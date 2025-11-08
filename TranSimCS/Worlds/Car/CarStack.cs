using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Save2;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Worlds.Car {
    public class CarStack : ObjectStack<Car, CarStack> {
        private readonly TSWorld world;
        public readonly TrackerSpatial<Car, CarStack> trackerSpatial;

        public CarStack(TSWorld world) : base(world) {
            this.world = world;
            trackerSpatial = new TrackerSpatial<Car, CarStack>(world);
            stackTrackers.Add(trackerSpatial);
        }

        public override Car ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            ObjPos? pos = null;
            string? mesh = null;

            var objPosConverter = new ObjPosConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "id":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "pos":
                        pos = objPosConverter.Read(ref reader0, typeof(ObjPos), options);
                        break;
                    case "mesh":
                        reader0.Read();
                        mesh = reader0.GetString();
                        break;
                }
            });

            if (guid == null) throw new JsonException("Missing id property");
            if (pos == null) throw new JsonException($"Missing pos property for car {guid}");
            Car car = new Car(world);
            car.Guid = guid.Value;
            car.PositionProp.Value = pos.Value;
            car.MeshId = mesh;
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

            writer.WriteEndObject();
        }
    }
}
