using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Arch.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS;
using TranSimCS.Roads;
using TranSimCS.Worlds;
using TranSimCS.Worlds.ECS;

public class Program {
    public static string DataRoot { get; private set; }
    public static readonly JsonConverter laneConverter = new LaneConverter();
    public static readonly JsonConverter nodeConverter = new RoadNodeConverter();
    public static readonly JsonConverter laneEndConverter = new LaneEndConverter();
    public static readonly JsonConverter nodeEndConverter = new RoadNodeEndConverter();
    public static readonly JsonConverter laneStripConverter = new LaneStripConverter();
    public static readonly JsonConverter roadStripConverter = new RoadStripConverter();
    public static readonly JsonConverter worldConverter = new TSWorldConverter();

    private static void Main(string[] args) {
        var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        DataRoot = Path.Combine(appdata, "TranSim");
        Directory.CreateDirectory(DataRoot);

        

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(laneConverter);
        settings.Converters.Add(nodeConverter);
        settings.Converters.Add(laneEndConverter);
        settings.Converters.Add(nodeEndConverter);
        settings.Converters.Add(laneStripConverter);
        settings.Converters.Add(roadStripConverter);
        settings.Converters.Add(worldConverter);
        var serializer = JsonSerializer.Create(settings);

        LaneSpec laneSpec = LaneSpec.Default;
        string laneSpecPath = Path.Combine(DataRoot, "laneSpec.json");
        SerializeToFile<LaneSpec>(laneSpecPath, laneSpec, serializer);

        TSWorld exampleWorld = new TSWorld();
        TSWorld.SetUpExampleWorld(exampleWorld);
        var node = exampleWorld.RoadNodes.First(roadNode => roadNode.Name.StartsWith("Fancy"));
        string roadNodePath = Path.Combine(DataRoot, "roadNode.json");
        SerializeToFile<RoadNode>(roadNodePath, node, serializer);

        var worldPath = Path.Combine(DataRoot, "world.json");
        SerializeToFile<TSWorld>(worldPath, exampleWorld, serializer);

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
}