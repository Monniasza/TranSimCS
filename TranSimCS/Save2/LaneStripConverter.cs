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
            LaneEnd? start = null;
            LaneEnd? end = null;
            LaneSpec spec = LaneSpec.Default;

            var laneEndConverter = new LaneEndConverter(_world);
            var laneSpecConverter = new LaneSpecConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "start":
                        start = laneEndConverter.Read(ref reader0, typeof(LaneEnd), options);
                        break;
                    case "end":
                        end = laneEndConverter.Read(ref reader0, typeof(LaneEnd), options);
                        break;
                    case "spec":
                        spec = laneSpecConverter.Read(ref reader0, typeof(LaneSpec), options);
                        break;
                }
            });

            if (start == null) JsonProcessor.Fail(reader, "Missing start property");
            if (end == null) JsonProcessor.Fail(reader, "Missing end property");

            var laneStrip = new LaneStrip(start.Value, end.Value);
            laneStrip.Spec = spec;
            return laneStrip;
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
