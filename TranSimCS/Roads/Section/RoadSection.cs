using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Section {
    public class RoadSection : Obj, IObjMesh<RoadSection>{
        //Added nodes, maintained by the road section
        private List<RoadNodeEnd> nodes = new();
        public IList<RoadNodeEnd> Nodes => new ReadOnlyCollection<RoadNodeEnd>(nodes);

        //Section contents
        public readonly Property<RoadNodeEndPair> MainSlopeNodes;
        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }

        //Cached contents
        public MeshGenerator<RoadSection> Mesh { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Normal { get; private set; }
        public Mesh Surface { get; private set; }

        public RoadSection() {
            MainSlopeNodes = new Property<RoadNodeEndPair>(new(null, null), "slopeNodes", this);
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            Mesh = new MeshGenerator<RoadSection>(this, (rs, mesh) => SectionRenderer.GenerateSectionMesh(rs, mesh));
            PropertyChanged += RoadSection_PropertyChanged;
        }

        private void RoadSection_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Mesh.Invalidate();
        }
        internal void OnConnect(RoadNodeEnd node) {
            nodes.Add(node);

            if (nodes.Count == 1) {
                MainSlopeNodes.Value = new(node, node);
            } else if (nodes.Count == 2) {
                MainSlopeNodes.Value = new(MainSlopeNodes.Value.Start, node);
            }

            node.Node.PropertyChanged += Node_PropertyChanged;
            Regenerate();
        }

        internal void OnDisconnect(RoadNodeEnd node) {
            nodes.Remove(node);
            node.Node.PropertyChanged -= Node_PropertyChanged;

            //If there are fewer than 2 node, demolish this
            if(nodes.Count < 2) {
                World.RoadSections.data.Remove(this);
            }

            // If one of the main-slope road node ends was disconnected, select the closest one to the existing other half
            SegmentHalf? affectedHalf = null;
            var pair = MainSlopeNodes.Value;

            if (pair.Start == node) affectedHalf = SegmentHalf.Start;
            if (pair.End == node) affectedHalf = SegmentHalf.End;

            if (affectedHalf != null) {
                RoadNodeEnd otherEnd = affectedHalf == SegmentHalf.Start ? pair.End : pair.Start;
                var replacement = otherEnd;
                var closestDistance = float.PositiveInfinity;

                // Reference for nearest search should be the existing other half if present, otherwise the disconnected node's position
                var referencePos = otherEnd?.CenterPosition ?? node.CenterPosition;

                foreach (var candidate in nodes) {
                    if (candidate == otherEnd) continue;
                    var candidatePos = candidate.CenterPosition;
                    var distance = Vector3.DistanceSquared(candidatePos, referencePos);
                    if (distance < closestDistance) {
                        replacement = candidate;
                        closestDistance = distance;
                    }
                }

                if (affectedHalf == SegmentHalf.Start) pair.Start = replacement;
                if (affectedHalf == SegmentHalf.End) pair.End = replacement;

                // Write back the updated pair to the property (struct copy)
                MainSlopeNodes.Value = pair;
            }

            Regenerate();
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            Regenerate();
        }


        public void Regenerate() {
            Mesh.Invalidate();

            if (nodes.Count == 0) return;

            //Find the center of mass and the normal
            Center = Vector3.Zero;
            Normal = Vector3.Zero;
            foreach (var node in nodes) {
                var frame = node.PositionProp.Value;
                var mat = frame.CalcReferenceFrame();
                Normal += mat.Y;
                Center += node.CenterPosition;
            }
            Center /= nodes.Count;
            if (Normal.LengthSquared() > 1e-6f) Normal.Normalize();
            else Normal = Vector3.Up;

            //Sort the nodes clockwise
            Comparison<RoadNodeEnd> comparer = CompareNodes2;
            nodes.Sort(comparer);
        }

        public int CompareNodes(RoadNodeEnd n1, RoadNodeEnd n2) {
            var p1 = n1.CenterPosition;
            var p2 = n2.CenterPosition;
            var det = (p1.X - Center.X) * (p2.Z - Center.Z) -
                (p2.X - Center.X) * (p1.Z - Center.Z);
            var reference = 0.0f;
            return det.CompareTo(reference);
        }
        public int CompareNodes2(RoadNodeEnd n1, RoadNodeEnd n2) {
            var r1 = Center - n1.CenterPosition;
            var r2 = Center - n2.CenterPosition;
            var a1 = Math.Atan2(r1.X, r1.Z);
            var a2 = Math.Atan2(r2.X, r2.Z);
            return a1.CompareTo(a2);
        }

        public int CompareNodes(Vector3 start, Vector3 end, Vector3 test) {
            var right = Vector3.Cross(end - start, Normal);
            var testOffset = test - Center;
            var reference = 0.0f;
            var det = Vector3.Dot(testOffset, right);
            return det.CompareTo(reference);
        }

        public LaneStrip[] FindStrips() {
            var result = new List<LaneStrip>();

            //Find connected road strips
            var roadStrips = new HashSet<RoadStrip>();
            foreach(var node in Nodes) roadStrips.AddRange(node.ConnectedSegments);

            //Filter the set
            roadStrips.FilterInPlace((strip) => Nodes.Contains(strip.StartNode) && Nodes.Contains(strip.EndNode));

            foreach(var road in roadStrips) result.AddRange(road.Lanes);

            return result.ToArray();
        }

        public RoadNodeEnd? GetNodeEnd() {
            throw new NotImplementedException();
        }
    }
}
