using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using System.Collections.ObjectModel;
using Arch.Core;

namespace TranSimCS.Worlds
{
    public partial class TSWorld{
        //The contents of the world
        public ObservableCollection<RoadStrip> RoadSegments { get; } = new();
        public ObservableCollection<RoadNode> RoadNodes { get; } = new();
        public ObservableCollection<RoadSection> RoadSections { get; } = new();
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
            RoadSegments.CollectionChanged += RoadSegments_CollectionChanged; // Subscribe to changes in the road segments collection
            RoadNodes.CollectionChanged += RoadNodes_CollectionChanged; // Subscribe to changes in the road nodes collection
            ECS = World.Create();
        }

        private void RoadSegments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road segments collection if needed
            // For example, you could log changes or update UI elements
            foreach (var segment in e.NewItems?.OfType<RoadStrip>() ?? Enumerable.Empty<RoadStrip>())
                HandleAddRoadSegment(segment); // Handle the addition of a new road segment
            foreach (var segment in e.OldItems?.OfType<RoadStrip>() ?? Enumerable.Empty<RoadStrip>())
                HandleRemoveRoadSegment(segment); // Handle the removal of a road segment
        }
        private void HandleAddRoadSegment(RoadStrip segment) {
            // Handle the addition of a new road segment
            segment.OnLaneRemoved += LaneRemovedFromRoad; // Subscribe to lane removal events in the road segment
            segment.OnLaneAdded += LaneAddedToRoad; // Subscribe to lane addition events in the road segment
            segment.StartNode.connectionsOld.Add(segment);
            segment.EndNode.connectionsOld.Add(segment);
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

        //Road nodes
        private void RoadNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road nodes collection if needed
            // For example, you could log changes or update UI elements
            foreach (var node in e.NewItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleAddRoadNode(node); // Handle the addition of a new road node
            foreach (var node in e.OldItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleRemoveRoadNode(node); // Handle the removal of a road node
        }
        private Dictionary<Guid, RoadNode> roadNodeIndex = new Dictionary<Guid, RoadNode>();
        public RoadNode FindRoadNode(Guid guid) => roadNodeIndex[guid];
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionProp.ValueChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionProp.ValueChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.Connections) {
                segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.Remove(segment); // Remove the segment from the road segments collection
            }
        }

        private void RoadNodePositionChanged(object sender, PropertyChangedEventArgs2<ObjPos> e) {
            if(sender is RoadNode node) {
                foreach(var segment in node.Connections) {
                    segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node position changes
                }
            }
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
