using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        public ObservableCollection<RoadNode> RoadNodes { get; } = new();

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
        public RoadNode? FindRoadNodeOrNull(Guid guid) => roadNodeIndex.GetValueOrDefault(guid, null);
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionProp.ValueChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
            var success = roadNodeIndex.TryAdd(node.Guid, node);
            if (success) Debug.Print($"Added road node {node.Guid}");
            else Debug.Print($"Node {node.Guid} already exists");
            //Check the get
            Debug.Print($"Contains road node? {FindRoadNode(node.Guid)}");

        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionProp.ValueChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.Connections) {
                segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.Remove(segment); // Remove the segment from the road segments collection
            }
            roadNodeIndex.Remove(node.Guid);
        }

        private void RoadNodePositionChanged(object sender, PropertyChangedEventArgs2<ObjPos> e) {
            if (sender is RoadNode node) {
                foreach (var segment in node.Connections) {
                    segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node position changes
                }
            }
        }
    }
}
