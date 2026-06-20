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
    public class RoadSection : Obj, IObjMesh<RoadSection>, IRoadFinish{
        //Added nodes, maintained by the road section
        private List<RoadNodeEnd> nodes = new();
        public IList<RoadNodeEnd> Nodes => new ReadOnlyCollection<RoadNodeEnd>(nodes);

        //Section contents
        public readonly Property<RoadNodeEndPair> MainSlopeNodes;
        public readonly Property<RoadFinish> FinishProperty;
        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }
        Property<RoadFinish> IRoadFinish.FinishProperty => FinishProperty;

        //Cached contents
        public MeshGenerator<RoadSection> Mesh { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Normal { get; private set; }
        public Mesh Surface { get; private set; }

        public RoadSection() {
            MainSlopeNodes = new(default, "slopeNodes", this);
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            Mesh = new MeshGenerator<RoadSection>(this, (rs, mesh) => SectionRenderer.GenerateSectionMesh(rs, mesh));
        }

        internal void OnConnect(RoadNodeEnd node) {
            nodes.Add(node);
            node.Node.PropertyChanged += Node_PropertyChanged;

            Regenerate();
        }

        internal void OnDisconnect(RoadNodeEnd node) {
            nodes.Remove(node);
            node.Node.PropertyChanged -= Node_PropertyChanged;

            //If there are fewer than 2 node, demolish this
            if(nodes.Count < 2) World.RoadSections.data.Remove(this);

            // If one of the main-slope road node ends was disconnected, select the closest one to the existing other half
            var mainSlope = MainSlopeNodes.Value;
            if(mainSlope.Start == node) mainSlope.Start = null;
            if(mainSlope.End == node) mainSlope.End = null;
            MainSlopeNodes.Value = mainSlope;

            Regenerate();
        }

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => Regenerate();


        public void Regenerate() {
            Mesh.Invalidate();

            if (nodes.Count == 0) return;




            //Find the center of mass and the normal
            var center = Vector3.Zero;
            var normal = Vector3.Zero;
            foreach (var node in nodes) {
                var frame = node.PositionProp.Value;
                var mat = frame.CalcReferenceFrame();
                normal += mat.Y;
                center += node.CenterPosition;
            }
            Center = (nodes.Count == 0) ? new(0, 0, 0) : center / nodes.Count;
            Normal = (normal.LengthSquared() > 1e-6f) ? normal.Normalized() : Vector3.UnitY;

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
    }
}
