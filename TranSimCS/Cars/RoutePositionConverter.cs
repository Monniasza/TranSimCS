using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Worlds;

namespace TranSimCS.Cars {
    public sealed class RoutePositionConverter(TSWorld world) : JsonConverter<RoutePosition> {
        public override RoutePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            float position = 0;
            List<RouteInput> rows = [];
            StripRefConverter stripConverter = new(world);
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "position":
                        reader0.Read();
                        position = reader0.GetSingle();
                        break;
                    case "strips":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            var strip = stripConverter.Read(ref reader1, typeof(LaneStrip), options);
                            reader1.Read();
                            var isReverse = reader1.GetBoolean();
                            rows.Add(new(strip, isReverse));
                        });
                        break;
                    default:
                        reader0.Skip();
                        break;
                }
            });
            if (!float.IsFinite(position) || position < 0) JsonProcessor.Fail(reader, $"Expected a valid nonnegative or no float position: {position}");
            if (rows.Count == 0) JsonProcessor.Fail(reader, "No lane strips in this RoutePosition");
            return new(new(rows), position);
        }

        public override void Write(Utf8JsonWriter writer, RoutePosition value, JsonSerializerOptions options) {
            var stripSerializer = new StripRefConverter(world);
            writer.WriteStartObject();
            writer.WriteNumber("position", value.Position);
            writer.WriteStartArray();
            foreach(var segment in value.Route.LaneStrips) {
                stripSerializer.Write(writer, segment.road, options);
                writer.WriteBooleanValue(segment.isReverse);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
