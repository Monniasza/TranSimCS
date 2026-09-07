using System;
using TranSimCS.Roads.Node;
using TranSimCS.TrafficLights;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        //Road nodes
        public RoadNode FindRoadNode(Guid guid) => Nodes.data.Find(guid);
        public RoadNode? FindRoadNodeOrNull(Guid guid) {
            var success = Nodes.data.TryFind(guid, out var node);
            if (success) return node;
            return null;
        }
        
        private void AddIfAbsent(RoadNode node) {
            if (Nodes.data.Contains(node)) return;
            Nodes.data.Add(node);
        }
        internal void AddIfAbsent(TrafficLightGroup tlg) {
            if (TrafficLights.data.Contains(tlg)) return;
            TrafficLights.data.Add(tlg);
        }
    }
}
