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

namespace TranSimCS.Worlds
{
    public partial class TSWorld{
        //The contents of the world
        public ListenableObjContainer<RoadStrip> RoadSegments { get; } = new();
        public ListenableObjContainer<RoadSection> RoadSections { get; } = new();
        public World ECS { get; private set; }

        public RoadStrip FindRoadStrip(RoadNodeEnd start, RoadNodeEnd end) {
            foreach (var strip in RoadSegments) 
                if (strip.CheckEnds(start, end)) 
                    return strip;
            return null;
        }
        public RoadStrip GetOrMakeRoadStrip(RoadNodeEnd start, RoadNodeEnd end) {
            RoadStrip result = FindRoadStrip(start, end);
            if (result == null) {
                result = new RoadStrip(start, end);
                RoadSegments.Add(result);
            }
            return result;
        }

        public LaneStrip FindLaneStrip(LaneEnd start, LaneEnd end) {
            var roadStrip = FindRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd);
            if (roadStrip == null) return null;
            foreach (var lane in roadStrip.Lanes) 
                if(lane.IsBetween(start, end)) return lane;
            return null;
        }
        public LaneStrip GetOrMakeLaneStrip(LaneEnd start, LaneEnd end) {
            var roadStrip = GetOrMakeRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd);
            foreach (var lane in roadStrip.Lanes)
                if (lane.IsBetween(start, end)) return lane;
            LaneStrip strip = new LaneStrip(start, end);
            roadStrip.AddLaneStrip(strip);
            return strip;
        }

        public TSWorld() {
            RoadSegments.ItemAdded += HandleAddRoadSegment;
            RoadSegments.ItemRemoved += HandleRemoveRoadSegment;
            RoadNodes.ItemAdded += HandleAddRoadNode;
            RoadNodes.ItemRemoved += HandleRemoveRoadNode;
            ECS = World.Create();
        }
        private void HandleAddRoadSegment(RoadStrip segment) {
            // Handle the addition of a new road segment
            segment.OnLaneRemoved += LaneRemovedFromRoad; // Subscribe to lane removal events in the road segment
            segment.OnLaneAdded += LaneAddedToRoad; // Subscribe to lane addition events in the road segment
            segment.StartNode.connectionsOld.Add(segment);
            segment.EndNode.connectionsOld.Add(segment);
            AddIfAbsent(segment.StartNode.Node);
            AddIfAbsent(segment.EndNode.Node);
        }

        private void AddIfAbsent(RoadNode node) {
            if(RoadNodes.Contains(node)) return;
            RoadNodes.Add(node);
        }

        private void HandleRemoveRoadSegment(RoadStrip segment) {
            // Handle the removal of a road segment
            segment.OnLaneAdded -= LaneAddedToRoad; // Unsubscribe from lane addition events in the road segment
            segment.OnLaneRemoved -= LaneRemovedFromRoad; // Unsubscribe from lane removal events in the road segment
            segment.StartNode.connectionsOld.Remove(segment);
            segment.EndNode.connectionsOld.Remove(segment);

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

        

        public void Update(float deltaTime)
        {

            // Update logic for the world can be added here
            foreach (var node in RoadNodes)
            {
                // Example: Update each road node's position or state
                
            }
        }

        public void ClearAll() {
            RoadSections.Clear();
            RoadSegments.Clear();
            RoadNodes.Clear();
            ECS.Clear();
        }
    }
}
