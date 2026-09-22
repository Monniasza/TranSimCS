using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Cars {
    public struct RoutePosition {
        public ImmutableArray<RouteInput> Route;
        public float Position;

        public RoutePosition(ImmutableArray<RouteInput> route, float position) {
            Route = route;
            Position = position;
        }
    }
}
