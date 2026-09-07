using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Roads;
using TranSimCS.Save2;
using TranSimCS.Worlds.Cars;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        // ===== SYSTEM.TEXT.JSON (NEW METHODS) =====

        /// <summary>
        /// Creates serialization options for System.Text.Json with all converters
        /// </summary>
        public JsonSerializerOptions CreateJsonOptions() {
            var options = new JsonSerializerOptions {
                WriteIndented = true, // Pretty JSON formatting
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            // Add converters
            options.Converters.Add(new Save2.LaneEndConverter(this));
            options.Converters.Add(new Save2.RoadNodeEndConverter(this));
            options.Converters.Add(new StripRefConverter(this));
            options.Converters.Add(new LanePositionConverter());
            options.Converters.Add(new Save2.Vector3Converter());
            options.Converters.Add(new ColorConverter());
            options.Converters.Add(new Save2.ObjPosConverter());
            options.Converters.Add(new Save2.LaneSpecConverter());
            options.Converters.Add(new Save2.LaneConverter());
            options.Converters.Add(new Save2.TSWorldConverter());
            options.Converters.Add(new Save2.Vector3iConverter());

            return options;
        }

        /// <summary>
        /// Saves the world to a file using System.Text.Json
        /// </summary>
        public void SaveToFileJson(string filename) {
            log.Info($"Saving world to {filename} using System.Text.Json");
            var options = CreateJsonOptions();

            try {
                string jsonString = System.Text.Json.JsonSerializer.Serialize(this, options);
                File.WriteAllText(filename, jsonString);
                log.Info($"World saved successfully to {filename}");
            } catch (Exception ex) {
                log.Error(ex, $"Failed to save world to {filename}");
                throw;
            }
        }

        public void LoadJsonData(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "nodes":
                        Nodes.data.Clear();
                        Nodes.ReadFromJson(ref reader0, options);
                        break;
                    case "segments":
                        RoadSegments.data.Clear();
                        RoadSegments.ReadFromJson(ref reader0, options);
                        break;
                    case "buildings":
                        Buildings.data.Clear();
                        Buildings.ReadFromJson(ref reader0, options);
                        break;
                    case "sections":
                        RoadSections.data.Clear();
                        RoadSections.ReadFromJson(ref reader0, options);
                        break;
                    case "cars":
                        Cars.data.Clear();
                        Cars.ReadFromJson(ref reader0, options);
                        break;
                    case "daytime":
                        reader0.Read();
                        DayTime = reader0.GetSingle();
                        break;
                    case "trafficlights":
                        TrafficLights.data.Clear();
                        TrafficLights.ReadFromJson(ref reader0, options);
                        break;
                }
            }, true);
        }

        /// <summary>
        /// Static method to load a world from a file using System.Text.Json
        /// </summary>
        public static TSWorld LoadJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            TSWorld world = new TSWorld();
            world.LoadJsonData(ref reader, options);
            return world;
        }
        public static TSWorld LoadFromFile(string filename) {
            TSWorld world = new TSWorld();
            world.ReadFromFile(filename);
            return world;
        }

        // Legacy method aliases for compatibility
        public void SaveToFile(string filename) => SaveToFileJson(filename);
        public void ReadFromFile(string filename) {
            log.Info($"Loading world from {filename} using System.Text.Json");
            var options = CreateJsonOptions();
            var readerOptions = new JsonReaderOptions {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            try {
                var byteArray = File.ReadAllBytes(filename);
                Utf8JsonReader reader = new(byteArray, readerOptions);
                LoadJsonData(ref reader, options);
            } catch (Exception ex) {
                log.Error(ex, $"Failed to load world from {filename}\n{ex}");
                throw;
            }
        }
        
    }
}
