using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;

namespace TranSimCS.Roads.Section {
    public sealed class SectionCache {
        public RoadSection Section { get; private set; }
        public Vector3 Center { get; private set; }
        public Vector3 Normal { get; private set; }
        public WorkingPlane WorkingPlane { get; private set; }
        public ImmutableArray<RoadNodeEnd> SortedNodes { get; private set; }

        internal SectionCache(RoadSection section) {
            Section = section;
            if (section.Nodes.Count == 0) return;

            //Find the center of mass and the normal
            var center = Vector3.Zero;
            var normal = Vector3.Zero;
            foreach (var node in section.Nodes) {
                var frame = node.PositionProp.Value;
                var mat = frame.CalcReferenceFrame();
                normal += mat.Y;
                center += node.CenterPosition;
            }
            Center = (section.Nodes.Count == 0) ? new(0, 0, 0) : center / section.Nodes.Count;
            Normal = (normal.LengthSquared() > 1e-6f) ? normal.Normalized() : Vector3.UnitY;

            //Sort the nodes clockwise
            SortedNodes = section.Nodes.Order(Comparer<RoadNodeEnd>.Create(CompareNodes2)).ToImmutableArray();

            //Find arbitrary vectors for the working plane
            var frame0 = SortedNodes[0].CalcReferenceFrame();
            var tangential = frame0.Z;
            var binormal = frame0.X;
            WorkingPlane = new(Center, binormal, tangential);

            Debug.Assert(Math.Abs(Vector3.Dot(WorkingPlane.X, Normal)) < 1e-4f);
            Debug.Assert(Math.Abs(Vector3.Dot(WorkingPlane.Y, Normal)) < 1e-4f);
            Debug.Assert(Math.Abs(Vector3.Dot(Vector3.Cross(
                WorkingPlane.X,
                WorkingPlane.Y),
                Normal)) > 0.99f);
        }

        public int CompareNodes2(RoadNodeEnd n1, RoadNodeEnd n2) {
            var r1 = Center - n1.CenterPosition;
            var r2 = Center - n2.CenterPosition;
            var a1 = Math.Atan2(r1.X, r1.Z);
            var a2 = Math.Atan2(r2.X, r2.Z);
            return a1.CompareTo(a2);
        }
    }
}
