using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class LaneStripConverter : JsonConverter<LaneStrip> {
        private readonly TSWorld _world;

        public LaneStripConverter(TSWorld world) {
            _world = world;
        }

        public override LaneStrip Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject token");
            }

            LaneEnd? start = null;
            LaneEnd? end = null;
            LaneSpec spec = LaneSpec.Default;

            var laneEndConverter = new LaneEndConverter(_world);
            var laneSpecConverter = new LaneSpecConverter();

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    if (start == null) throw new JsonException("Missing start property");
                    if (end == null) throw new JsonException("Missing end property");

                    var laneStrip = new LaneStrip(start.Value, end.Value);
                    laneStrip.Spec = spec;
                    return laneStrip;
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "start":
                            start = laneEndConverter.Read(ref reader, typeof(LaneEnd), options);
                            break;
                        case "end":
                            end = laneEndConverter.Read(ref reader, typeof(LaneEnd), options);
                            break;
                        case "spec":
                            spec = laneSpecConverter.Read(ref reader, typeof(LaneSpec), options);
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, LaneStrip value, JsonSerializerOptions options) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("start");
            var laneEndConverter = new LaneEndConverter(_world);
            laneEndConverter.Write(writer, value.StartLane, options);

            writer.WritePropertyName("end");
            laneEndConverter.Write(writer, value.EndLane, options);

            writer.WritePropertyName("spec");
            var laneSpecConverter = new LaneSpecConverter();
            laneSpecConverter.Write(writer, value.Spec, options);

            writer.WriteEndObject();
        }
    }
}
