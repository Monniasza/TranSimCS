using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public struct RoadNodeEndPair(RoadNodeEnd start, RoadNodeEnd end): IReadOnlyList<RoadNodeEnd> {
        public RoadNodeEnd Start = start;
        public RoadNodeEnd End = end;

        

        //Conversion to collections
        public (RoadNodeEnd, RoadNodeEnd) ToTuple => (Start, End);
        public RoadNodeEnd[] ToArray => [Start, End];
        public RoadNodeEnd GetElement(int index) {
            if (index == 0) return Start;
            if (index == 1) return End;
            throw new IndexOutOfRangeException();
        }
        public RoadNodeEnd GetElement(SegmentHalf index) {
            if (index == SegmentHalf.Start) return Start;
            if (index == SegmentHalf.End) return End;
            throw new IndexOutOfRangeException();
        }

        //Implementation of I(ReadOnly)List
        public int Count => 2;

        public IEnumerator<RoadNodeEnd> GetEnumerator() {
            IEnumerable<RoadNodeEnd> e = ToArray;
            return e.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        

        public IEnumerable<RoadNodeEnd> this[int key] => [GetElement(key)];

        RoadNodeEnd IReadOnlyList<RoadNodeEnd>.this[int index] => GetElement(index);
    }

    public class RoadSection : Obj {
        //Added nodes, maintained by the 
        private List<RoadNodeEnd> nodes = new();
        public IList<RoadNodeEnd> Nodes => new ReadOnlyCollection<RoadNodeEnd>(nodes);

        //Main slope nodes
        public readonly Property<RoadNodeEndPair> MainSlopeNodes;

        public Vector3 Center { get; private set; }

        public RoadSection() { 
            
        }

        internal void OnConnect(RoadNodeEnd node) {
            nodes.Add(node);
            node.Node.PropertyChanged += Node_PropertyChanged;
            Regenerate();
        }

        internal void OnDisconnect(RoadNodeEnd node) {
            nodes.Remove(node);
            node.Node.PropertyChanged -= Node_PropertyChanged;

            //If one of the main-slope road node ends was disconnected, select the closest one to the existing
            SegmentHalf? affectedHalf = null;
            var pair = MainSlopeNodes.Value;
            if (pair.Start == node) affectedHalf = SegmentHalf.Start;
            if (pair.End == node) affectedHalf = SegmentHalf.End;

            if (affectedHalf != null) {
                RoadNodeEnd otherEnd = (affectedHalf == SegmentHalf.Start) ? pair.End : pair.Start;
                var replacement = otherEnd;
                var closestDistance = float.PositiveInfinity;
                var currPosition = node.CenterPosition;
                foreach (var candidate in nodes) {
                    if(candidate == otherEnd) continue;
                    var centerPosition = candidate.CenterPosition;
                    var distance = Vector3.DistanceSquared(centerPosition, centerPosition);
                    if (distance < closestDistance) {
                        replacement = candidate;
                        closestDistance = distance;
                    }
                }
                if (affectedHalf == SegmentHalf.Start) pair.Start = replacement;
                if (affectedHalf == SegmentHalf.End) pair.End = replacement;
            }

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
