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

        public static readonly Property<bool> InvertAllNormalsProp;
        public static bool InvertAllNormals { get => InvertAllNormalsProp.Value; set => InvertAllNormalsProp.Value = value; }

        public static readonly Property<bool> ShowGroundProp;
        public static bool ShowGround { get => ShowGroundProp.Value; set => ShowGroundProp.Value = value; }

        static Settings(){
            RoadAccuracyProp = new(17, "roadAccuracy", null);
            RoadAccuracyProp.ValidateChanges += (s, e) => {
                if (e.NewValue < 2) throw new ArgumentException("Accuracy must be at least 2");
            };
            InvertAllNormalsProp = new(false, "invertNormals");
            ShowGroundProp = new(true, "showGround");
        }

        public static SettingsData GetAll() => new SettingsData() {
            RoadAccuracy = RoadAccuracy,
            InvertAllNormals = InvertAllNormals,
            ShowGround = ShowGround
        };
        public static void SetAll(SettingsData data) {
            RoadAccuracy = data.RoadAccuracy;
            InvertAllNormals = data.InvertAllNormals;
            ShowGround = data.ShowGround;
        }
    }

    public struct SettingsData {
        public static SettingsData Default => new() {
            RoadAccuracy = 17,
            InvertAllNormals = false,
            ShowGround = true
        };

        public int RoadAccuracy;
        public bool InvertAllNormals;
        public bool ShowGround;
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
                    case "invertNormals":
                        reader0.Read();
                        data.InvertAllNormals = reader0.GetBoolean();
                        break;
                    case "showGround":
                        reader0.Read();
                        data.ShowGround = reader0.GetBoolean();
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

            writer.WritePropertyName("invertNormals");
            writer.WriteBooleanValue(value.InvertAllNormals);

            writer.WritePropertyName("showGround");
            writer.WriteBooleanValue(value.ShowGround);

            writer.WriteEndObject();
        }
    }
}
