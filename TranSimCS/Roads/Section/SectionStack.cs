using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Save2;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Roads.Section {
    public class SectionStack : ObjectStack<RoadSection, SectionStack> {
        public readonly TrackerSpatial<RoadSection, SectionStack> trackerSpatial;
        public SectionStack(TSWorld world) : base(world) {
            trackerSpatial = new(world);
            stackTrackers.Add(trackerSpatial);
        }

        public override RoadSection ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            Guid? guid = null;
            RoadNodeEnd? start = null;
            RoadNodeEnd? end = null;
            var list = new List<RoadNodeEnd>();
            var nodeEndConverter = new RoadNodeEndConverter(World);
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propName) => {
                switch (propName.ToLower()) {
                    case "guid":
                        guid = JsonSerializer.Deserialize<Guid>(ref reader0, options);
                        break;
                    case "start":
                        reader0.Read();
                        start = nodeEndConverter.Read(ref reader0, typeof(RoadNodeEnd), options);
                        break;
                    case "end":
                        reader0.Read();
                        end = nodeEndConverter.Read(ref reader0, typeof(RoadNodeEnd), options);
                        break;
                    case "nodes":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => { 
                            var node = nodeEndConverter.Read(ref reader1, typeof(RoadNodeEnd), options);
                            list.Add(node);
                        });
                        break;
                    //TODO finish
                }
            });
            if (guid == null) JsonProcessor.Fail(reader, "Missing id property");
            if (start == null) JsonProcessor.Fail(reader, $"Missing start node for section {guid}");
            if (end == null) JsonProcessor.Fail(reader, $"Missing end node for section {guid}");
            if (list.Count == 0) JsonProcessor.Fail(reader, $"No attached nodes for section {guid}");
            RoadSection section = new RoadSection();
            section.Guid = guid.Value;
            foreach (var node in list) 
                node.ConnectedSection.Value = section;
            section.MainSlopeNodes.Value = new(start, end);
            return section;
            
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, RoadSection obj, JsonSerializerOptions options) {
            var nodeEndConverter = new RoadNodeEndConverter(World);

            writer.WriteStartObject();

            writer.WritePropertyName("guid");
            JsonSerializer.Serialize(writer, obj.Guid, options);

            writer.WritePropertyName("nodes");
            JsonSerializer.Serialize(writer, obj.Nodes, options);

            writer.WritePropertyName("start");
            nodeEndConverter.Write(writer, obj.MainSlopeNodes.Value.Start, options);

            writer.WritePropertyName("end");
            nodeEndConverter.Write(writer, obj.MainSlopeNodes.Value.End, options);

            //TODO finish

            writer.WriteEndObject();
        }
    }
}
