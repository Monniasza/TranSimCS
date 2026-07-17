using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    internal class LaneStripCache {
        //The lane strip
        public LaneStrip LaneStrip { get; private set; }
        public LaneStripCache(LaneStrip laneStrip) {
            Debug.Assert(laneStrip != null, "Creating a LaneStripCache for null");
            LaneStrip = laneStrip;
        }
        public void Invalidate() {
            _asphaltCache = null;
            _drivableAreaCache = null;
            _centerLUT = null;
            _lateralLUT = null;
            _lines = null;
        }

        //Caches
        private RoadSplineComponent? _asphaltCache;
        public RoadSplineComponent AsphaltCache => _asphaltCache ??= GenerateAsphaltStrip();
        private RoadSplineComponent? _drivableAreaCache;
        public RoadSplineComponent DrivableAreaCache => _drivableAreaCache ??= GenerateDrivableCache();
        private SplineLUT? _centerLUT;
        public SplineLUT CenterLUT => _centerLUT ??= new SplineLUT(AsphaltCache.Strip.Middle);
        private SplineLUT? _lateralLUT;
        public SplineLUT LateralLUT => _lateralLUT ??= new SplineLUT(AsphaltCache.Strip.right - AsphaltCache.Strip.left);
        private ImmutableArray<RoadSplineComponent>? _lines;
        public ImmutableArray<RoadSplineComponent> Lines => _lines ??= StripRenderer.GenerateStripEdgeLines(LaneStrip, 0.05f).ToImmutableArray();
        private ImmutableArray<RoadSplineComponent>? _allStrips;
        public ImmutableArray<RoadSplineComponent> AllStrips => _allStrips ??= GenerateStripList();

        private ImmutableArray<RoadSplineComponent> GenerateStripList(){
            var builder = new List<RoadSplineComponent>();
            builder.Add(AsphaltCache);
            builder.Add(DrivableAreaCache);
            builder.AddRange(Lines);
            return builder.ToImmutableArray();
        }
            

        private RoadSplineComponent GenerateDrivableCache() {
            var linewidth = LaneStrip.Spec.LineWidth;
            var tag = LaneStrip.Tag();
            var startl = tag.startRange.Min + linewidth;
            var endl = tag.endRange.Min + linewidth;
            var startr = tag.startRange.Max - linewidth;
            var endr = tag.endRange.Max - linewidth;
            if(endl > endr) endl = endr = (endl + endr) / 2;
            if(startl > startr) startl = startr = (startr + startl) / 2;
            tag.startRange = new(startl, startr);
            tag.endRange = new(endl, endr);
            var splineStrip = RoadRenderer.GenerateSplines(tag);
            return new() {
                Bias = 0.5f,
                Color = Color.Transparent,
                Strip = splineStrip,
                Type = RoadSplineComponentType.DrivingAreaMarker
            };
        }

        //Generation methods
        private RoadSplineComponent GenerateAsphaltStrip() {
            var splineStrip = RoadRenderer.GenerateSplines(LaneStrip.Tag());
            return new() {
                Bias = 0.5f,
                Color = LaneStrip.Spec.Color,
                Strip = splineStrip,
                Type = RoadSplineComponentType.Asphalt
            };
        }
    }
}
