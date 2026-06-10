using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Microsoft.Xna.Framework;
using NLog;
using NLog.Targets;
using TranSimCS;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.StripGenerator;
using TranSimCS.Save2;
using TranSimCS.Tools;
using TranSimCS.Worlds;
using TranSimCS.Worlds.ECS;

public class Program {
    public static string UserRoot { get; private set; }
    public static string SaveRoot { get; private set; }
    public static string LogRoot { get; private set; }

    public static string DataRoot { get; private set; }

    private static Logger log = LogManager.GetCurrentClassLogger();

    private static void Main(string[] args) {
        //NumberFormatInfo: Enforce the period as a decimal separator
        var nfi = NumberFormatInfo.CurrentInfo;
        nfi = (NumberFormatInfo) nfi.Clone();
        var culture = CultureInfo.CurrentCulture;
        culture = (CultureInfo) culture.Clone();
        nfi.NumberDecimalSeparator = ".";
        nfi.CurrencyDecimalSeparator = ".";
        nfi.PercentDecimalSeparator = ".";
        culture.NumberFormat = nfi;
        CultureInfo.CurrentCulture = culture;

        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        UserRoot = Path.Combine(appdata, "TranSim");
        SaveRoot = Path.Combine(UserRoot, "saves");
        LogRoot = Path.Combine(UserRoot, "logs");
        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        DataRoot = Path.GetDirectoryName(exePath);
        
        Directory.CreateDirectory(UserRoot);
        Directory.CreateDirectory(SaveRoot);
        Directory.CreateDirectory(LogRoot);

        var now = DateTime.Now;
        var nowString = now.ToString();
        var logPath = Path.Combine(LogRoot, "log"+nowString+".txt");

        NLog.LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
            builder.ForLogger().FilterMinLevel(LogLevel.Trace).WriteToFile(
                fileName: logPath);
        });

        log.Info("Running from " + DataRoot);

        //Add spline generators
        StripSplineGenerator.typeRegistry.converters["isotropic"] = IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(ClassicStripSplineGenerator.Instance);
        StripSplineGenerator.typeRegistry.converters["anisotropic"] = IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(AnisotropicStripSplineGenerator.Instance);

        JsonProcessor.Init();
        InspectMethods.Init();

        Game1.Start(args);
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