using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Property;
using TranSimCS.Save2;

namespace TranSimCS.Setting {
    public static class Settings {
        public static readonly Property<int> RoadAccuracyProp;
        public static int RoadAccuracy { get => RoadAccuracyProp.Value; set => RoadAccuracyProp.Value = value; }

        static Settings(){
            RoadAccuracyProp = new(65, "roadAccuracy", null);
            RoadAccuracyProp.ValidateChanges += (s, e) => {
                if (e.NewValue < 2) throw new ArgumentException("Accuracy must be at least 2");
            };
        }

        public static SettingsData GetAll() => new SettingsData() {
            RoadAccuracy = RoadAccuracy,
        };
        public static void SetAll(SettingsData data) {
            RoadAccuracy = data.RoadAccuracy;
        }
    }

    public struct SettingsData {
        public static SettingsData Default => new() {
            RoadAccuracy = 65
        };

        public int RoadAccuracy;
    }

    public class SettingsDataConverter : JsonConverter<SettingsData> {
        public override SettingsData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            SettingsData data = SettingsData.Default;
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "roadAccuracy":
                        reader0.Read();
                        data.RoadAccuracy = reader0.GetInt32();
                        break;
                    default:
                        reader0.Skip();
                        break;
                }
            });
            return data;
        }

        public override void Write(Utf8JsonWriter writer, SettingsData value, JsonSerializerOptions options) {
            writer.WriteStartObject();

            writer.WritePropertyName("roadAccuracy");
            writer.WriteNumberValue(value.RoadAccuracy);

            writer.WriteEndObject();
        }
    }
}
