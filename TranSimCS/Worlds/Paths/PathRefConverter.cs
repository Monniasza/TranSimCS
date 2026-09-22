using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arch.Core;
using TranSimCS.Roads.Node;
using TranSimCS.Save2;

namespace TranSimCS.Worlds.Paths {
    public class PathRefConverter(TSWorld world) : JsonConverter<SplinePath> {
        public override SplinePath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            HalfLane? startLaneEnd, endLaneEnd;

            var laneEndConverter = new LaneEndConverter(world);

            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            if(reader.TokenType == JsonTokenType.String) {
                var guid = Guid.Parse(reader.GetString()!);
                return world.Paths.FindPath(guid);
            }
            if (reader.TokenType != JsonTokenType.StartArray) JsonProcessor.FailTokenTypes(ref reader, JsonTokenType.Null, JsonTokenType.StartArray, JsonTokenType.String);

            startLaneEnd = laneEndConverter.Read(ref reader, typeof(HalfLane), options);
            endLaneEnd = laneEndConverter.Read(ref reader, typeof(HalfLane), options);

            if (startLaneEnd == null) JsonProcessor.Fail(reader, "Start lane not found");
            if (endLaneEnd == null) JsonProcessor.Fail(reader, "End lane not found");

            JsonProcessor.AssertTokenType(ref reader, JsonTokenType.EndArray);

            return world.GetOrMakeLaneStrip(startLaneEnd, endLaneEnd).Path;
        }

        public override void Write(Utf8JsonWriter writer, SplinePath value, JsonSerializerOptions options) {
            writer.WriteStringValue(value.Guid);
        }
    }
}
