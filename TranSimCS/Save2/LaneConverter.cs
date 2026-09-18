using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Save2 {
    public class LaneConverter : JsonConverter<LaneNode> {
        public override LaneNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Read(ref reader, typeToConvert, options, out _, out _);

        //Also reads the front/rear half-lane traffic light toggles saved alongside the lane's geometry (see HalfLane.HasTrafficLight).
        //Missing properties (e.g. loading an older save) default to false, matching the toggle's default.
        public LaneNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options, out bool frontLight, out bool rearLight) {
            float leftPosition = 0;
            float rightPosition = 0;
            LaneSpec spec = LaneSpec.None;
            Guid? guid = null;
            bool front = false, rear = false;
            var laneSpecConverter = new LaneSpecConverter();

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "left":
                        leftPosition = reader0.GetSingle();
                        break;
                    case "right":
                        rightPosition = reader0.GetSingle();
                        break;
                    case "spec":
                        spec = laneSpecConverter.Read(ref reader0, typeof(LaneSpec), options);
                        break;
                    case "guid":
                        guid = reader0.GetGuid();
                        break;
                    case "frontlight":
                        front = reader0.GetBoolean();
                        break;
                    case "rearlight":
                        rear = reader0.GetBoolean();
                        break;
                }
            });

            frontLight = front;
            rearLight = rear;
            return LaneNode.FromBounds(spec, new(leftPosition, rightPosition), guid);
        }

        public override void Write(Utf8JsonWriter writer, LaneNode value, JsonSerializerOptions options) =>
            Write(writer, value, options, false, false);

        //Also writes the front/rear half-lane traffic light toggles alongside the lane's geometry (see HalfLane.HasTrafficLight).
        public void Write(Utf8JsonWriter writer, LaneNode value, JsonSerializerOptions options, bool frontLight, bool rearLight) {
            if (value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            var range = value.Bounds;
            writer.WriteNumber("left", range.Min);
            writer.WriteNumber("right", range.Max);
            
            writer.WritePropertyName("spec");
            var laneSpecConverter = new LaneSpecConverter();
            laneSpecConverter.Write(writer, value.LaneSpec, options);

            writer.WritePropertyName("guid");
            writer.WriteStringValue(value.ID);

            writer.WriteBoolean("frontLight", frontLight);
            writer.WriteBoolean("rearLight", rearLight);
            
            writer.WriteEndObject();
        }
    }
}
