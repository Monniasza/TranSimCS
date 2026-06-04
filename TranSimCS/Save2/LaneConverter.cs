using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Save2 {
    public class LaneConverter : JsonConverter<LaneNode> {
        public override LaneNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            float leftPosition = 0;
            float rightPosition = 0;
            LaneSpec spec = LaneSpec.None;
            Guid? guid = null;
            var laneSpecConverter = new LaneSpecConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "left":
                        leftPosition = reader0.GetSingle();
                        break;
                    case "right":
                        rightPosition = reader0.GetSingle();
                        break;
                    case "spec":
                        spec = laneSpecConverter.Read(ref reader0, typeof(LaneSpec), options);
                        break;
                    case "guid":
                        guid = reader0.GetGuid();
                        break;
                }
            });

            return LaneNode.FromBounds(spec, new(leftPosition, rightPosition), guid);
        }

        public override void Write(Utf8JsonWriter writer, LaneNode value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var range = value.Bounds;
            writer.WriteNumber("left", range.Min);
            writer.WriteNumber("right", range.Max);
            
            writer.WritePropertyName("spec");
            var laneSpecConverter = new LaneSpecConverter();
            laneSpecConverter.Write(writer, value.LaneSpec, options);

            writer.WritePropertyName("guid");
            writer.WriteStringValue(value.ID);
            
            writer.WriteEndObject();
        }
    }
}
