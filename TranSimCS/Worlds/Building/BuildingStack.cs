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

namespace TranSimCS.Worlds.Building {
    public class BuildingStack : ObjectStack<BuildingUnit, BuildingStack> {
        public readonly TrackerSpatial<BuildingUnit, BuildingStack> spatial;

        public BuildingStack(TSWorld world): base(world) {
            spatial = new(world);
            stackTrackers.Add(spatial);
        }

        public override BuildingUnit ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            ObjPos? pos = null;
            Vector3i? size = null;
            Guid? guid = null;
            var objPosConverter = new ObjPosConverter();
            var vectorConverter = new Vector3iConverter();
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "id":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString()!);
                        break;
                    case "pos":
                        pos = objPosConverter.Read(ref reader0, typeof(ObjPos), options);
                        break;
                    case "size":
                        size = vectorConverter.Read(ref reader0, typeof(Vector3i), options);
                        break;
                }
            });
            if (guid == null) throw new JsonException("Missing id property");
            if (pos == null) throw new JsonException($"Missing pos property for building {guid}");
            if (size == null) throw new JsonException($"Missing size property for building {guid}");
            BuildingUnit building = new();
            building.Guid = guid.Value;
            building.UnitSizeProp.Value = size.Value;
            building.PositionProp.Value = pos.Value;
            return building;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, BuildingUnit value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteString("id", value.Guid.ToString());

            writer.WritePropertyName("pos");
            var objPosConverter = new ObjPosConverter();
            objPosConverter.Write(writer, value.PositionProp.Value, options);

            writer.WritePropertyName("size");
            var vecConverter = new Vector3iConverter();
            objPosConverter.Write(writer, value.PositionProp.Value, options);

            writer.WriteEndObject();
        }
    }
}
