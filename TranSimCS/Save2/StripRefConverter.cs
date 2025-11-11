using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Save2 {
    public class StripRefConverter(TSWorld world) : JsonConverter<LaneStrip> {
        public override LaneStrip? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var readerX = reader;
            if (readerX.TokenType == JsonTokenType.Null) return null;
            readerX.Read();
            if (readerX.TokenType == JsonTokenType.Null) return null;

            var elements = new List<LaneEnd>(2);
            var laneEndConverter = new LaneEndConverter(world);
            JsonProcessor.ReadJsonArrayProperties(ref reader, (ref reader0, idx) => {
                elements.Add(laneEndConverter.Read(ref reader0, typeof(LaneEnd), options));
            });
            if (elements.Count < 0) JsonProcessor.Fail(reader, "Fewer than 2 elements for the lane strip");
            return world.FindLaneStrip(elements[0], elements[1]);
        }

        public override void Write(Utf8JsonWriter writer, LaneStrip value, JsonSerializerOptions options) {
            if(value == null) {
                writer.WriteNullValue();
                return;
            }

            var laneEndConverter = new LaneEndConverter(world);
            writer.WriteStartArray();
            laneEndConverter.Write(writer, value.StartLane, options);
            laneEndConverter.Write(writer, value.EndLane, options);
            writer.WriteEndArray();
        }
    }
}
