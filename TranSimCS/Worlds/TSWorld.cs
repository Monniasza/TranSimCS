using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using System.Collections.ObjectModel;
using Arch.Core;
using System.Diagnostics;
using TranSimCS.Collections;
using NLog;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds.Building;
using TranSimCS.Worlds.Car;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.Section;
using TranSimCS.Property;
using TranSimCS.Setting;
using MonoGame.Extended;

namespace TranSimCS.Worlds
{
    public partial class TSWorld{
        private static Logger log = LogManager.GetCurrentClassLogger();

        //The contents of the world
        
        public BuildingStack Buildings { get; }
        public CarStack Cars { get; }

        private float _daytime;
        public float DayTime {
            get => _daytime;
            set => _daytime = ((value % 60) + 60) % 60;
        }

        public RoadStrip? FindRoadStrip(RoadNodeEnd start, RoadNodeEnd end) {
            foreach (var strip in RoadSegments.data) 
                if (strip.CheckEnds(start, end)) 
                    return strip;
            return null;
        }
        public RoadStrip GetOrMakeRoadStrip(RoadNodeEnd start, RoadNodeEnd end, RoadFinish? finish = null) {
            RoadStrip? result = FindRoadStrip(start, end);
            if (result == null) {
                result = new RoadStrip(start, end);
                result.Finish = finish ?? RoadFinish.Embankment;
                RoadSegments.data.Add(result);
            }
            return result;
        }

        public LaneStrip? FindLaneStrip(LaneEnd start, LaneEnd end) {
            var roadStrip = FindRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd);
            if (roadStrip == null) return null;
            foreach (var lane in roadStrip.Lanes) 
                if(lane.IsBetween(start, end)) return lane;
            return null;
        }
        public LaneStrip GetOrMakeLaneStrip(LaneEnd start, LaneEnd end, RoadFinish? finish = null, LaneSpec? laneSpec = null) {
            var roadStrip = GetOrMakeRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd, finish);
            foreach (var lane in roadStrip.Lanes)
                if (lane.IsBetween(start, end)) return lane;
            LaneStrip strip = new LaneStrip(start, end);
            strip.Spec = laneSpec ?? LaneSpec.Default;
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
        }

        


        //Every 60 frames, log tree parameters
        private int diagCounter = 0;

        public event Action<GameTime>? OnUpdate;
        public void Update(GameTime deltaTime){
            OnUpdate?.Invoke(deltaTime);

            // Update logic for the world can be added here
            DayTime += (60 / Settings.DayTimeLength) * deltaTime.GetElapsedSeconds();
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
