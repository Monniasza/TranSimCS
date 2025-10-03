using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;

namespace TranSimCS.Save2 {
    public class LaneConverter : JsonConverter<Lane> {
        public override Lane Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            float leftPosition = 0;
            float rightPosition = 0;
            LaneSpec spec = LaneSpec.None;

            var laneSpecConverter = new LaneSpecConverter();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return new Lane() {
                        LeftPosition = leftPosition,
                        RightPosition = rightPosition,
                        Spec = spec
                    };
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "left":
                            leftPosition = reader.GetSingle();
                            break;
                        case "right":
                            rightPosition = reader.GetSingle();
                            break;
                        case "spec":
                            spec = laneSpecConverter.Read(ref reader, typeof(LaneSpec), options);
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, Lane value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WriteNumber("left", value.LeftPosition);
            writer.WriteNumber("right", value.RightPosition);
            
            writer.WritePropertyName("spec");
            var laneSpecConverter = new LaneSpecConverter();
            laneSpecConverter.Write(writer, value.Spec, options);
            
            writer.WriteEndObject();
        }
    }
}
