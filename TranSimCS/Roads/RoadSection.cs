using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public class RoadSection : Obj {
        //Added nodes, maintained by the 
        private List<RoadNodeEnd> nodes = new();
        public IList<RoadNodeEnd> Nodes => new ReadOnlyCollection<RoadNodeEnd>(nodes);
        public Vector3 Center { get; private set; }

        public RoadSection() { 
            
        }

        internal void OnConnect(RoadNodeEnd node) {
            nodes.Add(node);
            node.Node.PropertyChanged += Node_PropertyChanged;
            Regenerate();
        }

        public void OnDisconnect(RoadNodeEnd node) {
            nodes.Remove(node);
            node.Node.PropertyChanged -= Node_PropertyChanged;
            Regenerate();
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Regenerate();
        }


        public void Regenerate() {
            InvalidateMesh();

            if (nodes.Count == 0) return;

            //Find the center of mass
            Center = Vector3.Zero;
            foreach (var node in nodes)
                Center += node.CenterPosition;
            Center /= nodes.Count;

            //Sort the nodes clockwise
            Comparison<RoadNodeEnd> comparer = (n1, n2) => {
                var p1 = n1.CenterPosition;
                var p2 = n2.CenterPosition;
                var det = (p1.X - Center.X) * (p2.Z - Center.Z) -
                    (p2.X - Center.X) * (p1.Z - Center.Z);
                var reference = 0.0f;
                return det.CompareTo(reference);
            };
            nodes.Sort(comparer);
        }

        protected override void GenerateMesh(Mesh mesh) {
            RoadRenderer.GenerateSectionMesh(this, mesh);
        }
    }
}
