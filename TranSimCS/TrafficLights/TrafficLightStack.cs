using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Save2;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.TrafficLights {
    public sealed class TrafficLightStack : ObjectStack<TrafficLightGroup, TrafficLightStack> {
        public TrackerSpatial<TrafficLightGroup, TrafficLightStack> trackerSpatial { get; private set; }
        public UpdateLoopTracker<TrafficLightGroup, TrafficLightStack> trackerUpdate { get; private set; }
        public TrafficLightStack(TSWorld world): base(world) {
            this.trackerSpatial = new(world);
            this.trackerUpdate = new(TrafficLightGroup.Update);
            stackTrackers.Add(this.trackerSpatial);
            stackTrackers.Add(this.trackerUpdate);
        }

        public override TrafficLightGroup ReadElementFromJson(ref Utf8JsonReader reader, JsonSerializerOptions options) {
            var phaseConverter = new TrafficLightPhaseConverter(World);

            Guid? guid = null;
            List<RoadSection> sections = [];
            List<TrafficLightPhase> phases = [];
            int phaseNumber = 0;
            float timer = 0;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "sections":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            //JsonProcessor.ForceRead(ref reader1);
                            var sectionGuid = Guid.Parse(reader1.GetString());
                            sections.Add(World.RoadSections.data.Find(sectionGuid));
                        });
                        break;
                    case "phases":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            phases.Add(phaseConverter.Read(ref reader1, typeof(TrafficLightPhase), options));
                        });
                        break;
                    case "phasenumber":
                        reader0.Read();
                        phaseNumber = reader0.GetInt32();
                        break;
                    case "timer":
                        reader0.Read();
                        timer = reader0.GetSingle();
                        break;
                    case "guid":
                        reader0.Read();
                        guid = Guid.Parse(reader0.GetString());
                        break;
                    default:
                        reader0.Skip();
                        break;
                }
            });

            TrafficLightGroup result = new(guid);
            result.Phases.AddRange(phases);
            result.Time = timer;
            result.PhaseId = phaseNumber;
            foreach (var section in sections) if (section != null) section.TrafficLightGroup = result;
            return result;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, TrafficLightGroup obj, JsonSerializerOptions options) {
            var phaseConverter = new TrafficLightPhaseConverter(World);

            writer.WriteStartObject();
            writer.WriteString("guid", obj.Guid);
            writer.WritePropertyName("sections");
            writer.WriteStartArray();
            foreach(var section in obj.ControlledSections) {
                writer.WriteStringValue(section.Guid);
            }
            writer.WriteEndArray();
            writer.WriteNumber("phasenumber", obj.PhaseId);
            writer.WritePropertyName("phases");
            writer.WriteStartArray();
            foreach(var phase in obj.Phases) {
                phaseConverter.Write(writer, phase, options);
            }
            writer.WriteEndArray();
            writer.WriteNumber("timer", obj.Time);

            writer.WriteEndObject();
        }
    }
}
