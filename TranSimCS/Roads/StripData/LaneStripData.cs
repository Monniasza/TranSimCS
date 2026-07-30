using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;

namespace TranSimCS.Roads.StripData {
    public sealed class LaneStripData: IExtent {
        public RoadStripData Parent { get; private set; }
        public LaneSpec Spec { get; private set; }
        public LaneNode StartNode { get; private set; }
        public LaneNode EndNode { get; private set; }
        public object? Tag { get; private set; }
        internal LaneStripData(RoadStripData parent, RoadDataBuilder.LaneConnectionData lcd) {
            Parent = parent;
            Spec = lcd.LaneSpec;
            StartNode = lcd.StartNode;
            EndNode = lcd.EndNode;
            Tag = lcd.Tag;
        }

        //Caches
        private bool? _isReverse;
        public bool IsReverse => _isReverse ??= Parent.StartLanes != Parent.EndLanes && Parent.StartLanes.Contains(EndNode);
        private OrthodistantLUT? _centerLUT;
        public OrthodistantLUT CenterLUT => _centerLUT ??= GenerateCenterLineLUT();
        private OrthodistantLUT? GenerateCenterLineLUT() {
            var startT = StartNode.CenterPos;
            var endT = EndNode.CenterPos;
            if (IsReverse) DataUtil.Swap(ref startT, ref endT);
            var points = Parent.OrthodistantBasis.Offset(startT, -endT);
            return new OrthodistantLUT(points);
        }
        private DualRange? _bounds;
        public DualRange Bounds => _bounds ??= GenerateBounds();
        private DualRange GenerateBounds() {
            var startLane = StartNode;
            var endLane = EndNode;
            if (IsReverse) DataUtil.Swap(ref startLane, ref endLane);
            return new(startLane.Bounds, endLane.Bounds);
        }


        private GridMesh<Vector3, RoadSplineComponent>? _allStrips;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _allStrips ??= GenerateStripList();
        private GridMesh<Vector3, RoadSplineComponent> GenerateStripList() {
            //Accumulate components from listeners
            var generatedComponents = StripRenderer.GenerateStripSplineComponents(this);

            //Generate spline strips
            var vertcount = generatedComponents.Length * 2;
            var accuracy = Settings.RoadAccuracy;
            Vector3[,] vertices = new Vector3[vertcount, accuracy];

            var records = new GridCrossSectionalRecord<RoadSplineComponent>[generatedComponents.Length];

            //Generate a GridMesh
            for (int i = 0; i < vertcount; i += 2) {
                var j = i / 2;
                var component = generatedComponents[j];
                var surface = component.Item1;
                var range = component.Item2;
                var (lspline, rspline) = range.GenerateRoadSplineRange(Parent);
                for (int k = 0; k < accuracy; k++) {
                    vertices[i, k] = lspline[k];
                    vertices[i + 1, k] = rspline[k];
                }
                records[j] = new(i, i + 1, surface);
            }

            return new GridMesh<Vector3, RoadSplineComponent>(Immutable2DArray<Vector3>.Wrap(vertices), records.ToImmutableArray());
        }

        private MultiMesh? _mesh;
        public MultiMesh Mesh => _mesh ??= GenerateMesh();
        private MultiMesh GenerateMesh() {
            MultiMesh result = new();
            StripRenderer.GenerateLaneStripMesh(this, result);
            return result;
        }

        internal ExtentIndex? _extentIndex;
        public ExtentIndex GetLaneStripExtentIndex() {
            if( _extentIndex == null)  _ = Parent.LaneStripExtents; //generate extents
            Debug.Assert(_extentIndex != null, "Parent.LaneStripExtents didn't generate an index");
            return _extentIndex.Value;
        }

    }
}
