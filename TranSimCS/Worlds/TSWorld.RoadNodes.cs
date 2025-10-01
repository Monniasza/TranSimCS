using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;
using TranSimCS.Roads;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        public ListenableObjContainer<RoadNode> RoadNodes { get; } = new();

        //Road nodes
        public RoadNode FindRoadNode(Guid guid) => RoadNodes.Find(guid);
        public RoadNode? FindRoadNodeOrNull(Guid guid) {
            var success = RoadNodes.TryFind(guid, out var node);
            if (success) return node;
            return null;
        }
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionProp.ValueChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
            Debug.Print($"Road node id {node.Guid} name {node.Name} added");
        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionProp.ValueChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.Connections) {
                segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.Remove(segment); // Remove the segment from the road segments collection
            }
            Debug.Print($"Road node id {node.Guid} name {node.Name} removed");
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
