using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
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
            var laneEndConverter = new LaneEndConverter(World);
            var phaseConverter = new TrafficLightPhaseConverter(World);

            Guid? guid = null;
            List<HalfLane> lanes = [];
            List<TrafficLightPhase> phases = [];
            int phaseNumber = 0;
            float timer = 0;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "lanes":
                        JsonProcessor.ReadJsonArrayProperties(ref reader0, (ref reader1, idx) => {
                            lanes.Add(laneEndConverter.Read(ref reader1, typeof(HalfLane), options));
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
            result.CurrentPhase = phaseNumber;
            foreach (var lane in lanes) lane.TrafficLight = result;
            return result;
        }

        public override void SaveElementToJson(Utf8JsonWriter writer, TrafficLightGroup obj, JsonSerializerOptions options) {
            var laneEndConverter = new LaneEndConverter(World);
            var phaseConverter = new TrafficLightPhaseConverter(World);

            writer.WriteStartObject();
            writer.WriteString("guid", obj.Guid);
            writer.WritePropertyName("lanes");
            writer.WriteStartArray();
            foreach(var lane in obj.ControlledHalfLanes) {
                laneEndConverter.Write(writer, lane, options);
            }
            writer.WriteEndArray();
            writer.WriteNumber("phasenumber", obj.CurrentPhase);
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
