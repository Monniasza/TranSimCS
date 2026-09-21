using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
        /// <summary>
        /// Generates the centre line lookup table for the lane strip.
        /// <para>
        /// The centre line is normally derived from the road's spline. When the strip has been removed
        /// from its road, the road spline is no longer available, so the strip's own
        /// <see cref="SplinePath"/> is used instead. That path is orphaned rather than deleted when the
        /// strip is removed, so it still serves its last generated spline and traffic already on the
        /// strip can finish leaving it without a <see cref="NullReferenceException"/>.
        /// </para>
        /// </summary>
        /// <returns>The centre line lookup table.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the strip has no road and no path to fall back to.
        /// </exception>
        private OrthodistantLUT GenerateCenterLineLUT() {
            var startT = LaneStrip.StartLane.MiddlePosition;
            var endT = LaneStrip.EndLane.MiddlePosition;
            if (LaneStrip.IsReverse()) DataUtil.Swap(ref startT, ref endT);

            var road = LaneStrip.Road;
            if (road == null) {
                //The strip was removed from its road. Fall back to the strip's own path, which is
                //orphaned rather than deleted and therefore still serves its last generated spline.
                var path = LaneStrip.ExistingPath;
                if (path == null)
                    throw new InvalidOperationException("Cannot generate a centre line for a lane strip with no road and no path");
                return path.GetSpline();
            }

            var points = road.OrthodistantBasis.Offset(startT, -endT);
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
