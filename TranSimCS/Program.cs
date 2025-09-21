using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS.Roads;
using TranSimCS.Worlds.ECS;

public class Program {
    public static string DataRoot { get; private set; }
    public static readonly JsonConverter laneConverter = new LaneConverter();
    public static readonly JsonConverter nodeConverter = new RoadNodeConverter();

    private static void Main(string[] args) {
        DataRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/TranSim");
        Directory.CreateDirectory(DataRoot);

        LaneSpec laneSpec = LaneSpec.Default;

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(laneConverter);
        settings.Converters.Add(nodeConverter);
        var serializer = JsonSerializer.Create(settings);

        string filepath = Path.Combine(DataRoot, "laneSpec.json");
        using (var filestream = File.OpenWrite(filepath)) {
            var writer = new StreamWriter(filestream);
            serializer.Serialize(writer, laneSpec, typeof(LaneSpec));
            writer.Flush();
        }        

        using var game = new TranSimCS.Game1();
        game.Run();
    }

    public static void SerializeToFile<T>(string path, T obj, JsonSerializer serializer) {
        using (var filestream = File.OpenWrite(path)) {
            var writer = new StreamWriter(filestream);
            serializer.Serialize(writer, obj, typeof(T));
            writer.Flush();
        }
    }
}