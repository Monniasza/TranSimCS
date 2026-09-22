using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Save2 {
    public class LaneStripConverter : JsonConverter<LaneStrip> {
        private readonly TSWorld _world;

        public LaneStripConverter(TSWorld world) {
            _world = world;
        }

        public override LaneStrip Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            HalfLane? start = null;
            HalfLane? end = null;
            LaneSpec spec = LaneSpec.Default;
            SplinePath? path = null;

            var laneEndConverter = new LaneEndConverter(_world);
            var laneSpecConverter = new LaneSpecConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "start":
                        start = laneEndConverter.Read(ref reader0, typeof(HalfLane), options);
                        break;
                    case "end":
                        end = laneEndConverter.Read(ref reader0, typeof(HalfLane), options);
                        break;
                    case "spec":
                        spec = laneSpecConverter.Read(ref reader0, typeof(LaneSpec), options);
                        break;
                    case "path":
                        //The path claim
                        reader0.Read();
                        var pathGuid = Guid.Parse(reader0.GetString()!);
                        path = _world.Paths.FindPath(pathGuid);
                        break;
                }
            });

            if (start == null) JsonProcessor.Fail(reader, "Missing start property or dead lane");
            if (end == null) JsonProcessor.Fail(reader, "Missing end property or dead lane");

            var laneStrip = new LaneStrip(start, end);
            laneStrip.LaneSpec = spec;
            if(path != null) laneStrip.ClaimPath(path);
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
            laneSpecConverter.Write(writer, value.LaneSpec, options);

            writer.WriteString("path", value.Path.Guid.ToString());

            writer.WriteEndObject();
        }
    }
}
