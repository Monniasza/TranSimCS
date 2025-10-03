using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Targets;
using TranSimCS;
using TranSimCS.Roads;
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
        //

        //Set up logs
        //var target = new FileTarget(logPath);
        //target.Ar
        
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

        Game1.Start();
    }

    public static void SerializeToFile<T>(string path, T obj, JsonSerializer serializer) {
        log.Trace("Saved the data to " + path);
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj, typeof(T));
        File.WriteAllText(path, stringWriter.ToString());
    }

    public static T Await<T>(Task<T> task) {
        return task.GetAwaiter().GetResult();
    }

    public static void DoNothing() { }
}