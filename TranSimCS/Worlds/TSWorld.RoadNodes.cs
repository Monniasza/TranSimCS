using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Collections;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
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
        
        private void AddIfAbsent(RoadNode node) {
            if (Nodes.data.Contains(node)) return;
            Nodes.data.Add(node);
        }
    }
}
