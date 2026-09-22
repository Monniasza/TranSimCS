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

            //Paths are written before the road network, because lane strips reference their respective paths that
            //own them. The path GUID is what allows the same path to be reused after loading.
            var pathConverter = new SplinePathConverter(value);
            writer.WritePropertyName("paths");
            writer.WriteStartArray();
            foreach (var path in value.Paths.Paths.Values) {
                pathConverter.Write(writer, path, options);
            }
            writer.WriteEndArray();
            
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
