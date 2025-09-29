using System;
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

        // Create default laneSpec.json only if it doesn't exist
        string laneSpecPath = Path.Combine(DataRoot, "laneSpec.json");
        if (!File.Exists(laneSpecPath)) {
            LaneSpec laneSpec = LaneSpec.Default;
            var laneSerializer = JsonSerializer.CreateDefault();
            SerializeToFile<LaneSpec>(laneSpecPath, laneSpec, laneSerializer);
        }

        TSWorld exampleWorld = new TSWorld();
        var serializer = exampleWorld.CreateSerializer();

        WorldGenerator.SetUpExampleWorld(exampleWorld);

        // Create default roadNode.json only if it doesn't exist
        string roadNodePath = Path.Combine(DataRoot, "roadNode.json");
        if (!File.Exists(roadNodePath)) {
            var node = exampleWorld.RoadNodes.First(roadNode => roadNode.Name.StartsWith("Fancy"));
            SerializeToFile<RoadNode>(roadNodePath, node, serializer);
        }

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