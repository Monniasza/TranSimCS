using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads;

namespace TranSimCS.Save2 {
    public class RoadFinishConverter : JsonConverter<RoadFinish> {
        public override RoadFinish Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            RoadFinish result = RoadFinish.Embankment;
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                switch (key) {
                    case "surface":
                        reader0.Read();
                        result.subsurface = (Surface)reader0.GetInt32();
                        break;
                    case "angle":
                        reader0.Read();
                        result.angle = reader0.GetSingle();
                        break;
                    case "depth":
                        reader0.Read();
                        result.depth = reader0.GetSingle();
                        break;
                }
            });
            return result;
        }

        public override void Write(Utf8JsonWriter writer, RoadFinish value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteNumber("surface", (int)value.subsurface);
            writer.WriteNumber("angle", value.angle);
            writer.WriteNumber("depth", value.depth);
            writer.WriteEndObject();
        }
    }
}
