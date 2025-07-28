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

            //Find the center of mass
            foreach (var node in nodes) 
                Center += node.PositionProp.Value.Position;
            Center /= nodes.Count;

            //Sort the nodes clockwise
            var sortingList = new List<(RoadNodeEnd, float)>();
            foreach (var node in nodes) {
                var nodePos = node.PositionProp.Value.Position;
                var angle = MathF.Atan2(nodePos.Z - Center.Z, nodePos.X - Center.X);
                sortingList.Add((node, angle));
            }
            Comparison<(RoadNodeEnd, float)> comparer = (x, y) => (x.Item2.CompareTo(y.Item2));
            sortingList.Sort(comparer);
            nodes.Clear();
            nodes.AddRange(sortingList.ConvertAll((x) => x.Item1));
        }

        protected override void GenerateMesh(Mesh mesh) {
            RoadRenderer.GenerateSectionMesh(this, mesh);
        }
    }
}
