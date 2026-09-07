using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return TSWorld.LoadJson(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, TSWorld value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            writer.WritePropertyName("nodes");
            value.Nodes.SaveToJson(writer, options);
            
            writer.WritePropertyName("segments");
            value.RoadSegments.SaveToJson(writer, options);

            writer.WritePropertyName("sections");
            value.RoadSections.SaveToJson(writer, options);

            writer.WritePropertyName("buildings");
            value.Buildings.SaveToJson(writer, options);

            writer.WritePropertyName("cars");
            value.Cars.SaveToJson(writer, options);

            writer.WritePropertyName("trafficLights");
            value.TrafficLights.SaveToJson(writer, options);

            writer.WriteNumber("daytime", value.DayTime);
            
            writer.WriteEndObject();
        }
    }
}
