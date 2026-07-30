using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.StripData;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.StripData {
    public sealed class RoadStripData {
        public RoadFinish Finish {get; private set; }
        public IndexSpline IndexSpline { get; private set; }
        public NodeSpec StartLanes { get; private set; }
        public NodeSpec EndLanes { get; private set; }
        public PositionEulerAngles StartPos {  get; private set; }
        public PositionEulerAngles EndPos { get; private set; }
        public ImmutableDictionary<RoadDataBuilder.LaneConnectionData, LaneStripData> LaneConnections { get; private set; }

        //Caches
        private OrthodistantBasis? _splineFrame;
        public OrthodistantBasis OrthodistantBasis => _splineFrame ??= GenerateOrthodistantBasis();
        private OrthodistantBasis GenerateOrthodistantBasis() => IndexSpline.ToOrthodistantBasis(StartPos, EndPos);
        private DualRange? _bounds;
        public DualRange Bounds => _bounds ??= CalculateBounds();
        private DualRange CalculateBounds() {
            DualRange result = default;
            foreach(var strip in LaneConnections) 
                result |= strip.Value.Bounds;
            return result;
        }
        public bool IsSingleEnded => StartLanes == EndLanes;

        internal RoadStripData(RoadDataBuilder rdb) {
            Finish = rdb.RoadFinish;
            IndexSpline = rdb.IndexSpline;
            StartLanes = rdb.StartLanes;
            EndLanes = rdb.EndLanes;
            StartPos = rdb.StartPos;
            EndPos = rdb.EndPos;
            LaneConnections = rdb.LaneConnections
                .Select(x => new KeyValuePair<RoadDataBuilder.LaneConnectionData, LaneStripData>(x, new LaneStripData(this, x)))
                .ToImmutableDictionary();
        }

        private LaneStripExtents? _laneStripExtents;
        public LaneStripExtents LaneStripExtents => _laneStripExtents ??= GenerateExtents();
        private LaneStripExtents GenerateExtents() {
            const float Tolerance = 0.10f;

            var strips = LaneConnections.Values
                .OrderBy(x => x.Bounds.startRange.Max)
                .ToArray();

            var extentBuilder = ImmutableArray.CreateBuilder<LaneStripExtent>();
            var leftBounds = ImmutableHashSet.CreateBuilder<LaneStripData>();
            var rightBounds = ImmutableHashSet.CreateBuilder<LaneStripData>();

            if (strips.Length == 0)
                return new LaneStripExtents(
                    leftBounds.ToImmutable(),
                    rightBounds.ToImmutable(),
                    ImmutableArray<LaneStripExtent>.Empty);

            var currentExtent = ImmutableArray.CreateBuilder<LaneStripData>();

            LaneStripData previous = strips[0];

            previous._extentIndex = new(0, 0);

            currentExtent.Add(previous);

            int extentIndex = 0;

            for (int i = 1; i < strips.Length; i++) {
                var current = strips[i];

                bool sameExtent =
                    MathF.Abs(
                        current.Bounds.startRange.Min -
                        previous.Bounds.startRange.Max) <= Tolerance
                    &&
                    MathF.Abs(
                        current.Bounds.endRange.Max -
                        previous.Bounds.endRange.Min) <= Tolerance;

                if (!sameExtent) {
                    var finished = currentExtent.ToImmutable();

                    leftBounds.Add(finished[0]);
                    rightBounds.Add(finished[^1]);

                    extentBuilder.Add(new(finished));

                    currentExtent.Clear();
                    extentIndex++;
                }

                current._extentIndex = new(extentIndex, currentExtent.Count);

                currentExtent.Add(current);
                previous = current;
            }

            if (currentExtent.Count != 0) {
                var finished = currentExtent.ToImmutable();

                leftBounds.Add(finished[0]);
                rightBounds.Add(finished[^1]);

                extentBuilder.Add(new(finished));
            }

            return new LaneStripExtents(
                leftBounds.ToImmutable(),
                rightBounds.ToImmutable(),
                extentBuilder.ToImmutable());
        }
    }
}