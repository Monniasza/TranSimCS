using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Save2;
using TranSimCS.Worlds;

namespace TranSimCS.TrafficLights {
    public sealed class TrafficLightPhaseConverter(TSWorld world) : JsonConverter<TrafficLightPhase> {
        public override TrafficLightPhase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var laneEndConverter = new LaneEndConverter(world);
            float duration = 1;
            List<HalfLane> lanes = [];
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "duration":
                        reader0.Read();
                        duration = reader0.GetSingle();
                        break;
                    case "green":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            var lane = laneEndConverter.Read(ref reader1, typeof(HalfLane), options);
                            lanes.Add(lane);
                        });
                        break;
                    default:
                        JsonProcessor.Fail(reader0, "Unexpected property name: " + name);
                        break;
                }
            });
            return new(duration, lanes.ToImmutableHashSet());
        }

        public override void Write(Utf8JsonWriter writer, TrafficLightPhase value, JsonSerializerOptions options) {
            var laneEndConverter = new LaneEndConverter(world);
            writer.WriteStartObject();
            writer.WriteNumber("duration", value.Duration);
            writer.WritePropertyName("green");
            writer.WriteStartArray();
            foreach(var lane in value.GreenLanes)
                laneEndConverter.Write(writer, lane, options);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
