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
            LaneEnd startLaneEnd, endLaneEnd;

            var laneEndConverter = new LaneEndConverter(world);

            reader.Read();
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType != JsonTokenType.StartArray) JsonProcessor.FailTokenTypes(ref reader, JsonTokenType.Null, JsonTokenType.StartArray);

            startLaneEnd = laneEndConverter.Read(ref reader, typeof(LaneEnd), options);
            endLaneEnd = laneEndConverter.Read(ref reader, typeof(LaneEnd), options);

            JsonProcessor.AssertTokenType(ref reader, JsonTokenType.EndArray);
            
            return world.GetOrMakeLaneStrip(startLaneEnd, endLaneEnd);
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
