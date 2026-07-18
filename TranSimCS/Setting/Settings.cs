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
        public static readonly Property<bool> DayNightCycleProp;
        public static bool DayNightCycle { get => DayNightCycleProp.Value; set => DayNightCycleProp.Value = value; }
        public static readonly Property<bool> SpawnCarsProp;
        public static bool SpawnCars { get => SpawnCarsProp.Value; set => SpawnCarsProp.Value = value; }
        public static readonly Property<float> CarSpawnRateProp;
        public static float CarSpawnRate { get => CarSpawnRateProp.Value; set => CarSpawnRateProp.Value = value; }

        public static readonly Property<float> DayTimeLengthProp;
        public static float DayTimeLength { get => DayTimeLengthProp.Value; set => DayTimeLengthProp.Value = value; }

        static Settings(){
            RoadAccuracyProp = new(17, "roadAccuracy", null);
            RoadAccuracyProp.ValidateChanges += (s, old, value) => {
                if (value < 2) throw new ArgumentException("Accuracy must be at least 2");
            };
            InvertAllNormalsProp = new(false, "invertNormals");
            ShowGroundProp = new(true, "showGround");
            DayNightCycleProp = new(true, "dayNightCycle");
            SpawnCarsProp = new(false, "spawnCars");
            CarSpawnRateProp = new(0.2f, "carFreq");
            DayTimeLengthProp = new(60, "dayTimeLength");
        }

        public static SettingsData GetAll() => new SettingsData() {
            RoadAccuracy = RoadAccuracy,
            InvertAllNormals = InvertAllNormals,
            ShowGround = ShowGround,
            DayNightCycle = DayNightCycle,
            SpawnCars = SpawnCars,
            CarSpawnRate = CarSpawnRate,
            DayTimeLength = DayTimeLength
        };
        public static void SetAll(SettingsData data) {
            RoadAccuracy = data.RoadAccuracy;
            InvertAllNormals = data.InvertAllNormals;
            ShowGround = data.ShowGround;
            DayNightCycle = data.DayNightCycle;
            SpawnCars = data.SpawnCars;
            CarSpawnRate = data.CarSpawnRate;
            DayTimeLength = data.DayTimeLength;
        }
    }

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
