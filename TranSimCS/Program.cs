using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arch.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS;
using TranSimCS.Roads;
using TranSimCS.Worlds;
using TranSimCS.Worlds.ECS;

public class Program {
    public static string DataRoot { get; private set; }
    public static string SaveRoot { get; private set; }

    private static void Main(string[] args) {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DataRoot = Path.Combine(appdata, "TranSim");
        SaveRoot = Path.Combine(DataRoot, "saves");
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(SaveRoot);

        //The guid bug
        var guidString = "a80b070f-9f9a-4049-b43b-5a2026b64c66";
        var guid = Guid.Parse(guidString);
        var dictionary = new Dictionary<Guid, string>();
        dictionary.Add(guid, "test");
        var retrievedString = dictionary[guid];
        Debug.Print(retrievedString);

        using var game = new TranSimCS.Game1();
        game.Run();
    }

    public static void SerializeToFile<T>(string path, T obj, JsonSerializer serializer) {
        Debug.Print("Saved the data to " + path);
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj, typeof(T));
        File.WriteAllText(path, stringWriter.ToString());
    }

    public static T Await<T>(Task<T> task) {
        return task.GetAwaiter().GetResult();
    }

    public static void DoNothing() { }
}