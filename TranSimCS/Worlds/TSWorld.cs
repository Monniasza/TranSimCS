using System;
using TranSimCS.Roads;
using NLog;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds.Building;
using TranSimCS.Worlds.Cars;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.Section;
using TranSimCS.Property;
using TranSimCS.Setting;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Worlds
{
    public partial class TSWorld{
        private static Logger log = LogManager.GetCurrentClassLogger();

        //The contents of the world
        
        public BuildingStack Buildings { get; }
        public CarStack Cars { get; }
        public PathSystem Paths { get; }

        private float _daytime;
        public float DayTime {
            get => _daytime;
            set => _daytime = ((value % 60) + 60) % 60;
        }

        public RoadStrip? FindRoadStrip(HalfNode start, HalfNode end) {
            foreach (var strip in RoadSegments.data) 
                if (strip.CheckEnds(start, end)) 
                    return strip;
            return null;
        }
        public RoadStrip GetOrMakeRoadStrip(HalfNode start, HalfNode end, RoadFinish? finish = null) {
            RoadStrip? result = FindRoadStrip(start, end);
            if (result == null) {
                result = new RoadStrip(start, end);
                result.Finish = finish ?? RoadFinish.Embankment;
                RoadSegments.data.Add(result);
            }
            return result;
        }

        public LaneStrip? FindLaneStrip(HalfLane start, HalfLane end) {
            var roadStrip = FindRoadStrip(start.HalfNode, end.HalfNode);
            if (roadStrip == null) return null;
            foreach (var lane in roadStrip.Lanes) 
                if(lane.IsBetween(start, end)) return lane;
            return null;
        }
        public LaneStrip GetOrMakeLaneStrip(HalfLane start, HalfLane end, RoadFinish? finish = null, LaneSpec? laneSpec = null) {
            var roadStrip = GetOrMakeRoadStrip(start.HalfNode, end.HalfNode, finish);
            foreach (var lane in roadStrip.Lanes)
                if (lane.IsBetween(start, end)) return lane;
            LaneStrip strip = new LaneStrip(start, end);
            strip.LaneSpec = laneSpec ?? LaneSpec.Default;
            roadStrip.AddLaneStrip(strip);
            return strip;
        }

        public TSWorld() {
            RootGraph = new SceneGraph.SceneTree();
            RootIndex = new(RootGraph);

            Buildings = new BuildingStack(this);
            Nodes = new NodeStack(this);
            RoadSegments = new SegmentStack(this);
            RoadSections = new SectionStack(this);
            Cars = new CarStack(this);
            Paths = new(this);
            TrafficLights = new(this);

            //Spatial indexing
            TempSelectorsMesh = new Property<Model.MultiMesh>(new Model.MultiMesh(), "selectors", null, Equality.ReferenceEqualComparer<MultiMesh>());
            TempSelectors = new SceneGraph.SceneLeaf(new MeshProperty(TempSelectorsMesh));
            RootGraph.Add(TempSelectors);

            //Event handling
            RoadSegments.data.ItemAdded += HandleAddRoadSegment;
            RoadSegments.data.ItemRemoved += HandleRemoveRoadSegment;
            Nodes.data.ItemAdded += HandleAddRoadNode;
            Nodes.data.ItemRemoved += HandleRemoveRoadNode;
            RoadSections.data.ItemAdded += HandleAddRoadSection;
            RoadSections.data.ItemRemoved += HandleRemoveRoadSection;
            TrafficLights.data.ItemAdded += HandleAddTrafficLight;
        }

        


        //Every 60 frames, log tree parameters
        private int diagCounter = 0;

        public event Action<float>? OnUpdate;
        public void Update(float deltaTime){
            OnUpdate?.Invoke(deltaTime);

            // Update logic for the world can be added here
            DayTime += (60 / Settings.DayTimeLength) * deltaTime;
            diagCounter++;
            if(diagCounter >= 60) {
                diagCounter = 0;

                //Print AABB diagnostic
                log.Trace("AABB diagnostics:");
                var diagnostics = RootIndex.tree.GenerateDiagnostics();
                if(diagnostics.Length == 2) {
                    log.Trace("Left: {0}", diagnostics[0].ToString());
                    log.Trace("Right: {0}", diagnostics[1].ToString());
                }
            }
        }

        public void ClearAll() {
            RoadSections.data.Clear();
            RoadSegments.data.Clear();
            Nodes.data.Clear();
            Buildings.data.Clear();
            Cars.data.Clear();
        }
    }
}
