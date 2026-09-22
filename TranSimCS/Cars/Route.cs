using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.UnitsOfMeasure;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Cars {
    public record struct RouteInput(LaneStrip road, bool isReverse){
        public CarStripPosition ToCarStripPosition(float meters) => new CarStripPosition(road, meters, isReverse);
    }
    public static class RouteMethods {
        public static IEnumerable<CarStripPosition> FindNext(LaneStrip strip, bool isReverse, SegmentHalf half) {
            ArgumentNullException.ThrowIfNull(strip);
            if (isReverse) half = half.Inverse();
            var nextLane = strip.GetHalf(half);
            if (nextLane == null) return [];
            nextLane = nextLane.OppositeHalf;
            if (nextLane == null) return [];
            return nextLane.ConnectedLaneStrips.Select(FromEnd);
        }
        public static CarStripPosition FromEnd(LaneStripEnd laneStrip) {
            var isEntryFromEnd = laneStrip.half == SegmentHalf.End;
            return new(laneStrip.strip, 0, isEntryFromEnd);
        }
    }
}
