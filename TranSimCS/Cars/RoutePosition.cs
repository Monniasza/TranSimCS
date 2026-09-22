using System;
using System.Collections.Generic;
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
        public Route Route;
        public float Position;

        public RoutePosition(Route route, float position) {
            Route = route;
            Position = position;
        }
    }
}
