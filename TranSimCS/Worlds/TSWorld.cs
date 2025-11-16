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
using TranSimCS.Worlds.Property;
using TranSimCS.Worlds.Car;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.Section;

namespace TranSimCS.Worlds
{
    public partial class TSWorld{
        private static Logger log = LogManager.GetCurrentClassLogger();

        //The contents of the world
        public SegmentStack RoadSegments { get; }
        public SectionStack RoadSections { get; }
        public BuildingStack Buildings { get; }
        public CarStack Cars { get; }
        public NodeStack Nodes { get; }
        public World ECS { get; private set; }

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
        public LaneStrip GetOrMakeLaneStrip(LaneEnd start, LaneEnd end, RoadFinish? finish = null) {
            var roadStrip = GetOrMakeRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd, finish);
            foreach (var lane in roadStrip.Lanes)
                if (lane.IsBetween(start, end)) return lane;
            LaneStrip strip = new LaneStrip(start, end);
            roadStrip.AddLaneStrip(strip);
            return strip;
        }

        public TSWorld() {
            RootGraph = new SceneGraph.SceneTree();

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

            ECS = World.Create();
        }

        private void HandleAddRoadSegment(RoadStrip segment) {
            // Handle the addition of a new road segment
            segment.OnLaneRemoved += LaneRemovedFromRoad; // Subscribe to lane removal events in the road segment
            segment.OnLaneAdded += LaneAddedToRoad; // Subscribe to lane addition events in the road segment
            segment.StartNode.connectedSegments.Add(segment);
            segment.EndNode.connectedSegments.Add(segment);
            AddIfAbsent(segment.StartNode.Node);
            AddIfAbsent(segment.EndNode.Node);
        }

        

        private void HandleRemoveRoadSegment(RoadStrip segment) {
            // Handle the removal of a road segment
            segment.OnLaneAdded -= LaneAddedToRoad; // Unsubscribe from lane addition events in the road segment
            segment.OnLaneRemoved -= LaneRemovedFromRoad; // Unsubscribe from lane removal events in the road segment
            segment.StartNode.connectedSegments.Remove(segment);
            segment.EndNode.connectedSegments.Remove(segment);

            //Remove node connections that are no longer valid
            var lanes = segment.Lanes.ToArray();
            foreach(var lane in lanes){
                lane.Destroy();
            };
        }
        private void LaneAddedToRoad(object sender, RoadStripEventArgs e) {
            //Handle the addition of a new lane to a road segment
        }
        private void LaneRemovedFromRoad(object sender, RoadStripEventArgs e) {
            //Handle the removal of a lane from a road segment
        }


        public event Action<GameTime>? OnUpdate;
        public void Update(GameTime deltaTime){
            OnUpdate?.Invoke(deltaTime);
            // Update logic for the world can be added here
        }

        public void ClearAll() {
            RoadSections.data.Clear();
            RoadSegments.data.Clear();
            Nodes.data.Clear();
            Buildings.data.Clear();
            ECS.Clear();
        }
    }
}
