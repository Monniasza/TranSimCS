using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Cars {
    public record struct RouteInput(LaneStrip road, bool isReverse);
    public record struct RouteKey(LaneStrip road, bool isReverse, float StartPosition, float EndPosition) {
        public RouteKey(LaneStrip road, bool isReverse) : this(road, isReverse, 0, 0) { }
        public RouteKey(RouteInput input) : this(input.road, input.isReverse, 0, 0) { }

        public RouteInput ToRouteInput => new(road, isReverse);
        public CarStripPosition Project(float meters) => new(road, meters - StartPosition, isReverse);
    }
    public sealed class Route {
        //Data
        public ImmutableArray<RouteKey> LaneStrips { get; private set; }
        public Route(IEnumerable<RouteInput> roads) : this(roads.ToArray()) { }
        public Route(params RouteInput[] roads){
            ArgumentNullException.ThrowIfNull(roads, nameof(roads));
            if (!roads.Any()) {
                throw new ArgumentException(
                    "Route must contain at least one lane strip.",
                    nameof(roads)
                );
            }
            var elements = new RouteKey[roads.Length];
            float cumulativeDistance = 0;
            for(int i = 0; i < elements.Length; i++) {
                var road = roads[i];
                var startDist = cumulativeDistance;
                var endDist = startDist + road.road.SplineLUT.Length;
                elements[i] = new RouteKey(road.road, road.isReverse, startDist, endDist);
                cumulativeDistance = endDist;
            }
            LaneStrips = elements.ToImmutableArray();
        }

        //Operations
        public int Find(float meters) {
            int min = 0;
            int max = LaneStrips.Length - 1;
            while(min <= max) {
                int mid = (min + max) >> 1;
                var element = LaneStrips[mid];
                if (meters >= element.StartPosition && meters <= element.EndPosition) {
                    //Found a match
                    return mid;
                }
                if(meters < element.StartPosition) {
                    //Before the strip
                    max = mid - 1;
                } else {
                    //After the strip
                    min = mid + 1;
                }
            }
            //Not in range
            if (meters < LaneStrips[0].StartPosition) return -1;
            return LaneStrips.Length;
        }
        public CarStripPosition FindValue(float meters) {
            var idx = Find(meters);
            if (idx < 0) idx = 0;
            if (idx >= LaneStrips.Length) idx = LaneStrips.Length - 1;
            return LaneStrips[idx].Project(meters);
        }
        public Route? Pop(int count) {
            if (count <= 0) return this;
            int remaining = LaneStrips.Length - count;
            if (remaining <= 0) return null;

            var elements = new RouteInput[remaining];
            for(int i = 0; i < remaining; i++) 
                elements[i] = LaneStrips[i+count].ToRouteInput;

            return new Route(elements);
        }
        public Route Plan(float metersFromStart) {
            if (metersFromStart <= LaneStrips[^1].EndPosition) return this;
            var elements = LaneStrips.ToList();
            while (elements[^1].EndPosition < metersFromStart) {
                var element = elements[^1];
                //Plan a new element
                var candidates = RouteMethods.FindNext(element.road, element.isReverse, SegmentHalf.End).ToArray();
                if(candidates.Length == 0) {
                    break;
                }
                var next = (CarStripPosition)candidates.GetRandomElement();
                elements.Add(new(next.LaneStrip, next.IsReverse, element.EndPosition, element.EndPosition + next.LaneStrip.SplineLUT.Length));
            }
            return elements.ToRoute();
        }
        public float Length() => LaneStrips[^1].EndPosition;

        public Transform3 GetPosition(float arclength) {
            var currentStrip = FindValue(arclength);
            var positionLUT = currentStrip.GetPositionLookup();
            var xyzt = positionLUT[currentStrip.LaneArcLength];
            var xyz = xyzt.ToXYZ();
            VectorMethods.CheckVector(xyz, "xyz");
            var t = xyzt.W;
            if (!float.IsFinite(t)) throw new ArithmeticException("Invalid spline parameter");
            var referenceFrame = currentStrip.GetPositionFrame(t);
            var lateral = referenceFrame.X;
            VectorMethods.CheckVector(lateral, "lateral");
            var tangential = referenceFrame.Z;
            VectorMethods.CheckVector(tangential, "tangential");
            return referenceFrame;
        }
    }
    public static class RouteMethods {
        public static RouteInput ToRouteInput(this RouteKey key) => key.ToRouteInput;
        public static RouteKey ToRouteKey(this RouteInput input) => new RouteKey(input);

        public static Route ToRoute(this RouteInput[] inputs) => new Route(inputs);
        public static Route ToRoute(this IEnumerable<RouteInput> inputs) => new Route(inputs);
        public static Route ToRoute(this IEnumerable<RouteKey> inputs) => new Route(inputs.Select(x => x.ToRouteInput));
        public static IEnumerable<CarStripPosition> FindNext(LaneStrip strip, bool isReverse, SegmentHalf half) {
            if (isReverse) half = half.Inverse();
            var nextLane = strip.GetHalf(half);
            nextLane = nextLane.OppositeHalf;
            return nextLane.ConnectedLaneStrips.Select(FromEnd);
        }
        public static CarStripPosition FromEnd(LaneStripEnd laneStrip) {
            var isEntryFromEnd = laneStrip.half == SegmentHalf.End;
            return new(laneStrip.strip, 0, isEntryFromEnd);
        }
    }
}
