using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Range;
using TranSimCS.Setting;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Strip {
    internal class LaneStripCache {
        //The lane strip
        public LaneStrip LaneStrip { get; private set; }
        public LaneStripCache(LaneStrip laneStrip) {
            Debug.Assert(laneStrip != null, "Creating a LaneStripCache for null");
            LaneStrip = laneStrip;
        }
        public void Invalidate() {
            _extentIndex = null;
            _centerLUT = null;
            _allStrips = null;
            _extentIndex = null;
            _mesh = null;
        }

        //Caches
        private OrthodistantLUT? _centerLUT;
        public OrthodistantLUT CenterLUT => _centerLUT ??= GenerateCenterLineLUT();
        private GridMesh<Vector3, RoadSplineComponent>? _allStrips;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _allStrips ??= GenerateStripList();

        internal ExtentIndex? _extentIndex;
        public ExtentIndex ExtentIndex => _extentIndex ??= GetLaneStripExtentIndex();
        
        private MultiMesh? _mesh;
        public MultiMesh Mesh => _mesh ??= GenerateMesh();


        //Generation methods
        private OrthodistantLUT GenerateCenterLineLUT() {
            var startT = LaneStrip.StartLane.MiddlePosition;
            var endT = LaneStrip.EndLane.MiddlePosition;
            if (LaneStrip.IsReverse()) DataUtil.Swap(ref startT, ref endT);
            var points = LaneStrip.Road.OrthodistantBasis.Offset(startT, -endT);
            return new OrthodistantLUT(points);
        }
        private GridMesh<Vector3, RoadSplineComponent> GenerateStripList() {
            //Accumulate components from listeners
            var generatedComponents = StripRenderer.GenerateStripSplineComponents(LaneStrip);

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
                var (lspline, rspline) = range.GenerateRoadSplineRange(LaneStrip.Road);
                for (int k = 0; k < accuracy; k++) {
                    vertices[i, k] = lspline[k];
                    vertices[i + 1, k] = rspline[k];
                }
                records[j] = new(i, i + 1, surface);
            }
            return new GridMesh<Vector3, RoadSplineComponent>(Immutable2DArray<Vector3>.Wrap(vertices), records.ToImmutableArray());
        }
        private MultiMesh GenerateMesh() {
            MultiMesh result = new();
            StripRenderer.GenerateLaneStripMesh(LaneStrip, result);
            return result;
        }
        public ExtentIndex GetLaneStripExtentIndex() {
            if (_extentIndex == null) _ = LaneStrip.Road.Extents; //generate extents
            Debug.Assert(_extentIndex != null, "Parent.LaneStripExtents didn't generate an index");
            return _extentIndex.Value;
        }
    }
}
