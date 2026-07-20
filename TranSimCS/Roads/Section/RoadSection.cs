using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.SceneGraph;
using TranSimCS.Setting;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Section {
    public class RoadSection : Obj, IObjMesh, IRoadFinish, IDraggableObj{
        //Contents managed by TSWorld
        internal HashSet<RoadStrip> _containedSegments = new HashSet<RoadStrip>();
        public ReadOnlySet<RoadStrip> ContainedSegments => new(_containedSegments);

        //Added nodes, maintained by the road section
        private HashSet<RoadNodeEnd> nodes = new();
        public ReadOnlySet<RoadNodeEnd> Nodes => new(nodes);

        //Section contents
        public readonly Property<RoadNodeEndPair> MainSlopeNodes;
        public readonly Property<RoadFinish> FinishProperty;

        public event MeshInvalidationCallback GeometryChanged;

        public RoadFinish Finish { get => FinishProperty.Value; set => FinishProperty.Value = value; }
        Property<RoadFinish> IRoadFinish.FinishProperty => FinishProperty;

        //Cached contents
        public MeshGenerator<RoadSection> Mesh { get; private set; }
        public MeshGenerator<RoadSection> SelectionMesh { get; private set; }
        private SectionCache? _cache;
        public SectionCache Cache => _cache ??= new SectionCache(this);
        public Vector3 Center => Cache.Center;
        public Vector3 Normal => Cache.Normal;
        public WorkingPlane WorkingPlane => Cache.WorkingPlane;
        public ImmutableArray<RoadNodeEnd> SortedNodes => Cache.SortedNodes;

        public RoadSection() {
            MainSlopeNodes = new(default, "slopeNodes", this);
            FinishProperty = new(RoadFinish.Embankment, "finish", this);
            Mesh = new MeshGenerator<RoadSection>(this, SectionRenderer.GenerateSectionMesh);
            SelectionMesh = new MeshGenerator<RoadSection>(this, SectionRenderer.GenerateSectionSelectionMesh);
            SelectionMesh.OnMeshInvalidated += HandleMeshInvalidated;
        }

        private void HandleMeshInvalidated() {
            _cache = null;
            GeometryChanged?.Invoke(this);
        }

        internal void OnConnect(RoadNodeEnd node) {
            nodes.Add(node);
            FirePropertyEvent(this, new(PropertyNames.NodeOfSection));
        }

        internal void OnDisconnect(RoadNodeEnd node) {
            nodes.Remove(node);

            //If there are fewer than 1 node, demolish this
            if(nodes.Count < 1) World.RoadSections.data.Remove(this);

            // If one of the main-slope road node ends was disconnected, select the closest one to the existing other half
            var mainSlope = MainSlopeNodes.Value;
            if(mainSlope.Start == node) mainSlope.Start = null;
            if(mainSlope.End == node) mainSlope.End = null;
            MainSlopeNodes.Value = mainSlope;

            FirePropertyEvent(this, new(PropertyNames.NodeOfSection));
        }

        IPosition[] IDraggableObj.DraggableComponents() => Nodes.ToArray();

        public void GenerateGeometry(RenderTarget target) => target.Draw(Mesh.GetMesh());
        public BoundingBox GetBounds() => SelectionMesh.GetMesh().GetBounds();
        public bool ComputeIntersection(Ray ray, out float distance, out object? tag) => SelectionMesh.GetMesh().ComputeIntersection(ray, out distance, out tag);
    }
}
