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

        LaneSpec laneSpec = LaneSpec.Default;
        string laneSpecPath = Path.Combine(DataRoot, "laneSpec.json");
        var laneSerializer = JsonSerializer.CreateDefault();
        SerializeToFile<LaneSpec>(laneSpecPath, laneSpec, laneSerializer);

        TSWorld exampleWorld = new TSWorld();
        var serializer = exampleWorld.CreateSerializer();

        WorldGenerator.SetUpExampleWorld(exampleWorld);
        var node = exampleWorld.RoadNodes.First(roadNode => roadNode.Name.StartsWith("Fancy"));
        string roadNodePath = Path.Combine(DataRoot, "roadNode.json");
        SerializeToFile<RoadNode>(roadNodePath, node, serializer);

        using var game = new TranSimCS.Game1();
        game.Run();
    }

    public static void SerializeToFile<T>(string path, T obj, JsonSerializer serializer) {
        Debug.Print("Saved the data to " + path);
        using (var filestream = File.OpenWrite(path)) {
            var writer = new StreamWriter(filestream);
            serializer.Serialize(writer, obj, typeof(T));
            writer.Flush();
        }
    }

    public static T Await<T>(Task<T> task) {
        return task.GetAwaiter().GetResult();
    }

    public static void DoNothing() { }
}