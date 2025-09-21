using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranSimCS.Roads {
    public class LaneConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return typeof(Lane).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var lane = (Lane)value;
            writer.WriteStartObject();

            // Serialize properties of the Lane class
            writer.WritePropertyName("left");
            writer.WriteValue(lane.LeftPosition);
            writer.WritePropertyName("right");
            writer.WriteValue(lane.RightPosition);
            writer.WritePropertyName("spec");
            serializer.Serialize(writer, lane.Spec);

            writer.WriteEndObject();
        }
    }
}
