using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using NLog;
using NLog.Targets;
using TranSimCS;
using TranSimCS.Roads;
using TranSimCS.Save2;
using TranSimCS.Worlds;
using TranSimCS.Worlds.ECS;

public class Program {
    public static string DataRoot { get; private set; }
    public static string SaveRoot { get; private set; }
    private static Logger log = LogManager.GetCurrentClassLogger();

    private static void Main(string[] args) {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DataRoot = Path.Combine(appdata, "TranSim");
        SaveRoot = Path.Combine(DataRoot, "saves");
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(SaveRoot);

        var logPath = Path.Combine(DataRoot, "log.txt");

        NLog.LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToFile(
                fileName: logPath);
        });

        //The guid bug
        var guidString = "a80b070f-9f9a-4049-b43b-5a2026b64c66";
        var guid = Guid.Parse(guidString);
        var dictionary = new Dictionary<Guid, string>();
        dictionary.Add(guid, "test");
        var retrievedString = dictionary[guid];
        log.Trace(retrievedString);

        JsonProcessor.Init();

        Game1.Start();
    }

    // ===== SYSTEM.TEXT.JSON (NEW METHODS) =====

    /// <summary>
    /// Serializes an object to a file using System.Text.Json
    /// </summary>
    public static void SerializeToFileJson<T>(string path, T obj, System.Text.Json.JsonSerializerOptions? options = null) {
        log.Info($"Saving data to {path} using System.Text.Json");
        try {
            if (options == null) {
                options = new System.Text.Json.JsonSerializerOptions {
                    WriteIndented = true
                };
            }

            string jsonString = System.Text.Json.JsonSerializer.Serialize(obj, options);
            File.WriteAllText(path, jsonString);
            log.Info($"Data saved successfully to {path}");
        } catch (Exception ex) {
            log.Error(ex, $"Failed to save data to {path}");
            throw;
        }
    }

    /// <summary>
    /// Deserializes an object from a file using System.Text.Json
    /// </summary>
    public static T? DeserializeFromFileJson<T>(string path, System.Text.Json.JsonSerializerOptions? options = null) {
        log.Info($"Loading data from {path} using System.Text.Json");
        try {
            if (!File.Exists(path)) {
                throw new FileNotFoundException($"File not found: {path}");
            }

            string jsonString = File.ReadAllText(path);
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(jsonString, options);
            log.Info($"Data loaded successfully from {path}");
            return result;
        } catch (Exception ex) {
            log.Error(ex, $"Failed to load data from {path}");
            throw;
        }
    }

    public static T Await<T>(Task<T> task) {
        return task.GetAwaiter().GetResult();
    }

    public static void DoNothing() { }
}