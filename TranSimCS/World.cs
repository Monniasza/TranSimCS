using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using System.Collections.ObjectModel;

namespace TranSimCS
{
    public class World
    {
        public ObservableCollection<LaneConnection> RoadSegments { get; } = new();
        public ObservableCollection<RoadNode> RoadNodes { get; } = new();

        public World() {
            RoadSegments.CollectionChanged += RoadSegments_CollectionChanged; // Subscribe to changes in the road segments collection
            RoadNodes.CollectionChanged += RoadNodes_CollectionChanged; // Subscribe to changes in the road nodes collection
        }

        private void RoadSegments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road segments collection if needed
            // For example, you could log changes or update UI elements
            foreach (var segment in e.NewItems?.OfType<LaneConnection>() ?? Enumerable.Empty<LaneConnection>())
                HandleAddRoadSegment(segment); // Handle the addition of a new road segment
            foreach (var segment in e.OldItems?.OfType<LaneConnection>() ?? Enumerable.Empty<LaneConnection>())
                HandleRemoveRoadSegment(segment); // Handle the removal of a road segment
        }
        private void HandleAddRoadSegment(LaneConnection segment) {
            // Handle the addition of a new road segment
            segment.SpecChanged += RoadSegmentChanged; // Subscribe to changes in the road segment
        }
        private void HandleRemoveRoadSegment(LaneConnection segment) {
            // Handle the removal of a road segment
            segment.SpecChanged -= RoadSegmentChanged; // Unsubscribe from changes in the road segment                                        //Remove node connections that are no longer valid
            segment.StartNode = null; // Clear the start node reference
            segment.EndNode = null; // Clear the end node reference
        }

        private void RoadNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road nodes collection if needed
            // For example, you could log changes or update UI elements
            foreach (var node in e.NewItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleAddRoadNode(node); // Handle the addition of a new road node
            foreach (var node in e.OldItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleRemoveRoadNode(node); // Handle the removal of a road node
        }
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.connections) {
                segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.Remove(segment); // Remove the segment from the road segments collection
            }
        }

        private void RoadSegmentChanged(object sender, LaneConnectionChangedEventArgs e) {
            // Handle changes to a specific road segment
            // For example, you could log changes or update UI elements
            
        }
        private void RoadNodePositionChanged(object sender, NodePositionChangedEventArgs e) {
            if(sender is RoadNode node) {
                foreach(var segment in node.connections) {
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
    }
}
