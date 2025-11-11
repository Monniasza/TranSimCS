using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class TSWorldConverter : JsonConverter<TSWorld> {
        public override TSWorld Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var world = new TSWorld();
            
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "nodes":
                        world.Nodes.data.Clear();
                        world.Nodes.ReadFromJson(ref reader0, options);
                        break;
                    case "segments":
                        world.RoadSegments.data.Clear();
                        world.RoadSegments.ReadFromJson(ref reader0, options);
                        break;
                    case "buildings":
                        world.Buildings.data.Clear();
                        world.Buildings.ReadFromJson(ref reader0, options);
                        break;
                    case "sections":
                        world.RoadSections.data.Clear();
                        world.RoadSections.ReadFromJson(ref reader0, options);
                        break;
                    case "cars":
                        world.Cars.data.Clear();
                        world.Cars.ReadFromJson(ref reader0, options);
                        break;
                }
            }, true);

            return world;
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
            
            writer.WriteEndObject();
        }
    }
}
