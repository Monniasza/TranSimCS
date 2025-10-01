using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TranSimCS.Roads;

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
        public void ReadFromJSON(JsonReader jsonReader) {
            var serializer = CreateSerializer();
            serializer.Populate(jsonReader, this);
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
