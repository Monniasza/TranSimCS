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
                segment.SpecChanged += RoadSegmentChanged; // Subscribe to changes in the road segment
            foreach (var segment in e.OldItems?.OfType<LaneConnection>() ?? Enumerable.Empty<LaneConnection>())
                segment.SpecChanged -= RoadSegmentChanged; // Unsubscribe from changes in the road segment
        }
        private void RoadNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road nodes collection if needed
            // For example, you could log changes or update UI elements
            foreach (var node in e.NewItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                node.PositionChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
            foreach (var node in e.OldItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                node.PositionChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
        }
        private void RoadSegmentChanged(object sender, LaneConnectionChangedEventArgs e) {
            // Handle changes to a specific road segment
            // For example, you could log changes or update UI elements
            if(sender is LaneConnection segment) {
                var node1 = e.OldPosition.StartNode;
                var node2 = e.OldPosition.EndNode;
                var newNode1 = e.NewPosition.StartNode;
                var newNode2 = e.NewPosition.EndNode;
                node1.connections.Remove(segment); // Remove the old segment from the first node's connections
                node2.connections.Remove(segment); // Remove the old segment from the second node's connections
                newNode1.connections.Add(segment); // Add the new segment to the first node's connections
                newNode2.connections.Add(segment); // Add the new segment to the second node's connections
            }
            
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
