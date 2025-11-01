using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;
using TranSimCS.Roads;
using TranSimCS.Tools;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        //Road nodes
        public RoadNode FindRoadNode(Guid guid) => Nodes.data.Find(guid);
        public RoadNode? FindRoadNodeOrNull(Guid guid) {
            var success = Nodes.data.TryFind(guid, out var node);
            if (success) return node;
            return null;
        }
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionProp.ValueChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
            log.Trace($"Road node id {node.Guid} name {node.Name} added");
        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionProp.ValueChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.Connections) {
                segment.Mesh.Invalidate(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.data.Remove(segment); // Remove the segment from the road segments collection
            }
            log.Trace($"Road node id {node.Guid} name {node.Name} removed");
        }

        private void RoadNodePositionChanged(object sender, PropertyChangedEventArgs2<ObjPos> e) {
            if (sender is RoadNode node) {
                foreach (var segment in node.Connections) {
                    segment.Mesh.Invalidate(); // Invalidate the mesh of the segment if the node position changes
                }
            }
        }
        private void AddIfAbsent(RoadNode node) {
            if (Nodes.data.Contains(node)) return;
            Nodes.data.Add(node);
        }
    }
}
