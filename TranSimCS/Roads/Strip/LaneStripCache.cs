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
            _laneStripData = null;
        }

        //Caches
        public OrthodistantLUT CenterLUT => LaneStripData.CenterLUT;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => LaneStripData.AllStrips;

        private LaneStripData? _laneStripData;
        public LaneStripData LaneStripData => _laneStripData ??= GenerateLaneStripData();
        public MultiMesh Mesh => LaneStripData.Mesh;

        //Generation methods
        private LaneStripData GenerateLaneStripData() {
            var rsd = LaneStrip.Road.RoadStripData;
            var foundLSD = rsd.LaneConnections[LaneStrip.LaneConnectionData];
            Debug.Assert(foundLSD != null, "Lane not found in LaneStrip.Road.RoadStripData");
            return foundLSD;
        }
    }
}
