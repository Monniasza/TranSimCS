using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TranSimCS.Save;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class LaneStripConverter() : JsonConverter<LaneStrip> {

        public override LaneStrip ReadJson(JsonReader reader, Type objectType, LaneStrip existingValue, bool hasExistingValue, JsonSerializer serializer) {
            LaneEnd? start = null;
            LaneEnd? end = null;
            LaneSpec spec = LaneSpec.Default;
            if(existingValue != null) {
                start = existingValue.StartLane;
                end = existingValue.EndLane;
                spec = existingValue.Spec;
            }
            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "start": start = serializer.Deserialize<LaneEnd>(reader); break;
                    case "end": end = serializer.Deserialize<LaneEnd>(reader); break;
                    case "spec": spec = serializer.Deserialize<LaneSpec>(reader); break;
                }
            });
            return new LaneStrip(
                start ?? throw new JsonException("no start"),
                end ?? throw new JsonException("no end"),
                spec);
        }

        public override void WriteJson(JsonWriter writer, LaneStrip value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("start");
            serializer.Serialize(writer, value.StartLane);
            writer.WritePropertyName("end");
            serializer.Serialize(writer, value.EndLane);
            writer.WritePropertyName("spec");
            serializer.Serialize(writer, value.Spec);
            writer.WriteEndObject();
        }
    }
}
