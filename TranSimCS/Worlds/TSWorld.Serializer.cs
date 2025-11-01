using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Save2;

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
            options.Converters.Add(new Save2.Vector3Converter());
            options.Converters.Add(new Save2.ColorConverter());
            options.Converters.Add(new Save2.ObjPosConverter());
            options.Converters.Add(new Save2.LaneSpecConverter());
            options.Converters.Add(new Save2.LaneConverter());
            options.Converters.Add(new Save2.LaneEndConverter(this));
            options.Converters.Add(new Save2.RoadNodeEndConverter(this));
            options.Converters.Add(new Save2.LaneStripConverter(this));
            options.Converters.Add(new Save2.RoadStripConverter(this));
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

        /// <summary>
        /// Loads the world from a file using System.Text.Json
        /// </summary>
        public void LoadFromFileJson(string filename) {
            log.Info($"Loading world from {filename} using System.Text.Json");
            var options = CreateJsonOptions();

            try {
                string jsonString = File.ReadAllText(filename);
                var loadedWorld = System.Text.Json.JsonSerializer.Deserialize<TSWorld>(jsonString, options);

                if (loadedWorld != null) {
                    // Copy data from loaded world to this object
                    Nodes.data.Clear();
                    Nodes.data.UnionWith(loadedWorld.Nodes.data);

                    RoadSegments.Clear();
                    foreach (var segment in loadedWorld.RoadSegments) {
                        RoadSegments.Add(segment);
                    }

                    log.Info($"World loaded successfully from {filename}");
                } else {
                    throw new InvalidOperationException("Deserialization returned null");
                }
            } catch (Exception ex) {
                log.Error(ex, $"Failed to load world from {filename}\n{ex}");
                throw;
            }
        }

        /// <summary>
        /// Static method to load a world from a file using System.Text.Json
        /// </summary>
        public static TSWorld LoadJson(string filename) {
            TSWorld world = new TSWorld();
            world.LoadFromFileJson(filename);
            return world;
        }

        // Legacy method aliases for compatibility
        public void SaveToFile(string filename) => SaveToFileJson(filename);
        public void ReadFromFile(string filename) => LoadFromFileJson(filename);
        public static TSWorld Load(string filename) => LoadJson(filename);
    }
}
