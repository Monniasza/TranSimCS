using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS.Roads;
using TranSimCS.Save;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        public JsonSerializer CreateSerializer() {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new LaneConverter());
            settings.Converters.Add(new RoadNodeConverter(this));
            settings.Converters.Add(new LaneEndConverter(this));
            settings.Converters.Add(new RoadNodeEndConverter(this));
            settings.Converters.Add(new LaneStripConverter());
            settings.Converters.Add(new RoadStripConverter());
            settings.Converters.Add(new TSWorldConverter());
            return JsonSerializer.Create(settings);
        }
        public void ReadFromFile(string filePath) {
            using (var stream = File.OpenRead(filePath)) ReadFromStream(stream);
        }
        public void ReadFromStream(TextReader stream) => ReadFromJSON(new JsonTextReader(stream));
        public void ReadFromStream(Stream stream) => ReadFromStream(new StreamReader(stream));
        public void ReadFromJSON(JsonReader reader) {
            var serializer = CreateSerializer();
            JsonProcessor.ReadJsonObjectProperties(reader, key => {
                switch (key) {
                    case "nodes":
                        Debug.Print("Loading nodes");
                        var nodes = serializer.Deserialize<RoadNode[]>(reader);
                        RoadNodes.Clear();
                        foreach (var node in nodes ?? []) RoadNodes.Add(node);
                        break;
                    case "segments":
                        Debug.Print("Loading segments");
                        var segments = serializer.Deserialize<RoadStrip[]>(reader);
                        RoadSegments.Clear();
                        foreach (var segment in segments ?? []) RoadSegments.Add(segment);
                        break;
                }
            });
            Debug.Print("World loaded successfully");
        }

        public static TSWorld Load(string filename) {
            TSWorld world = new TSWorld();
            world.ReadFromFile(filename);
            return world;
        }

        public void SaveToFile(string filename) {
            var serializer = CreateSerializer();
            Program.SerializeToFile<TSWorld>(filename, this, serializer);
        }
    }
}
