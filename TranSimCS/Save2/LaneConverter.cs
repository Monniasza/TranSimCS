using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Save2 {
    public class LaneConverter : JsonConverter<Lane> {
        public override Lane Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            float leftPosition = 0;
            float rightPosition = 0;
            LaneSpec spec = LaneSpec.None;
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
                }
            });

            return new Lane() {
                LeftPosition = leftPosition,
                RightPosition = rightPosition,
                Spec = spec
            };
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
