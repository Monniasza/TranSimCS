using System;
using System.Collections.Immutable;
using System.Linq;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Roads.Range;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public class RoadStripCache {
        public RoadStrip RoadStrip { get; private set; }

        private LaneRange? _bounds;
        public LaneRange Bounds => _bounds ??= GenerateBounds();


        private OrthodistantBasis? _splineFrame;
        public OrthodistantBasis OrthodistantBasis => _splineFrame ??= GenerateOrthodistantBasis();
        private OrthodistantBasis GenerateOrthodistantBasis() => IndexStrip.ToOrthodistantBasis(RoadStrip.StartNode.PositionProp.Value, RoadStrip.EndNode.PositionProp.Value);


        private IndexSpline? _indexStrip;
        public IndexSpline IndexStrip => _indexStrip ??= GenerateIndexStrip();


        private Extents<LaneStrip>? _extents;
        public Extents<LaneStrip> Extents => _extents ??= GenerateExtents();
        

        public RoadStripCache(RoadStrip roadStrip) {
            RoadStrip = roadStrip;
        }

        private IndexSpline GenerateIndexStrip() {
            if (RoadStrip.IsSingleEnded()) {
                //The RoadStrip has only one end
                return RoadStrip.StartNode.GenerateDegenerateIndexStrips();
            } else {
                //The RoadStrip joins node-ends
                var startReference = RoadStrip.StartNode.Cache.ReferenceFrame;
                var endReference = RoadStrip.EndNode.Cache.ReferenceFrame;
                var range = RoadStrip.Bounds.ToDualRange();
                return RoadStrip.SplineGenerator.GenerateSplines(startReference, endReference, range);
            }
        }
        
        private LaneRange GenerateBounds() {
            Range<float> startRange = default;
            Range<float> endRange = default;
            foreach (var lane in RoadStrip.Lanes) {
                var startLane = lane.StartLane;
                var endLane = lane.EndLane;
                if (lane.IsReverse()) DataUtil.Swap(ref startLane, ref endLane);
                startRange = startRange.Union(startLane.Bounds);
                endRange = endRange.Union(endLane.Bounds);
            }
            return new(RoadStrip, startRange, endRange);
        }
        private Extents<LaneStrip> GenerateExtents() {
            const float Tolerance = 0.10f;

            var startOrder = RoadStrip.Lanes
                .OrderBy(x => x.Bounds.startRange.Max)
                .ThenByDescending(x => x.Bounds.endRange.Min)
                .ToArray();

            var extentBuilder = ImmutableArray.CreateBuilder<Extent<LaneStrip>>();
            var leftBounds = ImmutableHashSet.CreateBuilder<LaneStrip>();
            var rightBounds = ImmutableHashSet.CreateBuilder<LaneStrip>();

            if (startOrder.Length == 0)
                return new Extents<LaneStrip>(
                    leftBounds.ToImmutable(),
                    rightBounds.ToImmutable(),
                    ImmutableArray<Extent<LaneStrip>>.Empty);

            var currentExtent = ImmutableArray.CreateBuilder<LaneStrip>();

            LaneStrip previous = startOrder[0];

            previous._cache._extentIndex = new(0, 0);

            currentExtent.Add(previous);

            int extentIndex = 0;

            for (int i = 1; i < startOrder.Length; i++) {
                var current = startOrder[i];

                bool isAdjacentStart = MathF.Abs(
                        current.Bounds.startRange.Min -
                        previous.Bounds.startRange.Max) <= Tolerance
                        || previous.Bounds.startRange == current.Bounds.startRange;
                bool isAdjacentEnd = MathF.Abs(
                        current.Bounds.endRange.Max -
                        previous.Bounds.endRange.Min) <= Tolerance
                        || previous.Bounds.endRange == current.Bounds.endRange;

                bool sameExtent = isAdjacentStart && isAdjacentEnd;

                if (!sameExtent) {
                    var finished = currentExtent.ToImmutable();

                    leftBounds.Add(finished[0]);
                    rightBounds.Add(finished[^1]);

                    extentBuilder.Add(new(finished));

                    currentExtent.Clear();
                    extentIndex++;
                }

                current._cache._extentIndex = new(extentIndex, currentExtent.Count);

                currentExtent.Add(current);
                previous = current;
            }

            if (currentExtent.Count != 0) {
                var finished = currentExtent.ToImmutable();

                leftBounds.Add(finished[0]);
                rightBounds.Add(finished[^1]);

                extentBuilder.Add(new(finished));
            }

            return new Extents<LaneStrip>(
                leftBounds.ToImmutable(),
                rightBounds.ToImmutable(),
                extentBuilder.ToImmutable());
        }
    }
}
