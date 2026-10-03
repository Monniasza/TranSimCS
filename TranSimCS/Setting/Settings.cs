using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Save2;

namespace TranSimCS.Setting {
    public static class Settings {
        private static SettingsData _data = SettingsData.Default;

        public static ref SettingsData Data => ref _data;
        public static ref int RoadAccuracy => ref _data.RoadAccuracy;
        public static ref bool InvertAllNormals => ref _data.InvertAllNormals;
        public static ref bool ShowGround => ref _data.ShowGround;
        public static ref bool DayNightCycle => ref _data.DayNightCycle;
        public static ref bool SpawnCars => ref _data.SpawnCars;
        public static ref float CarSpawnRate => ref _data.CarSpawnRate;
        public static ref float DayTimeLength => ref _data.DayTimeLength;

        [Conditional("DEBUG")]
        public static void Validate() {
            Debug.Assert(_data.RoadAccuracy >= 2, "Accuracy must be at least 2");
        }

        public static SettingsData GetAll() {
            Validate();
            return _data;
        }

        public static void SetAll(SettingsData data) {
            _data = data;
            Validate();
        }
    }

    [JsonConverter(typeof(SettingsDataConverter))]
    public struct SettingsData {
        public static SettingsData Default => new() {
            RoadAccuracy = 17,
            InvertAllNormals = false,
            ShowGround = true,
            DayNightCycle = true,
            SpawnCars = false,
            CarSpawnRate = 0.2f,
            DayTimeLength = 60
        };

        public int RoadAccuracy;
        public bool InvertAllNormals;
        public bool ShowGround;
        public bool DayNightCycle;
        public bool SpawnCars;
        public float CarSpawnRate;
        public float DayTimeLength;
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
                    case "dayNightCycle":
                        reader0.Read();
                        data.DayNightCycle = reader0.GetBoolean();
                        break;
                    case "spawnCars":
                        reader0.Read();
                        data.SpawnCars = reader0.GetBoolean();
                        break;
                    case "carFreq":
                        reader0.Read();
                        data.CarSpawnRate = reader0.GetSingle();
                        break;
                    case "dayTimeLength":
                        reader0.Read();
                        data.DayTimeLength = reader0.GetSingle();
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

            writer.WriteBoolean("dayNightCycle", value.DayNightCycle);
            writer.WriteBoolean("spawnCars", value.SpawnCars);
            writer.WriteNumber("carFreq", value.CarSpawnRate);
            writer.WriteNumber("dayTimeLength", value.DayTimeLength);

            writer.WriteEndObject();
        }
    }
}
