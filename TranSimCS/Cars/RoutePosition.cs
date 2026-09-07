using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Cars {
    public struct RoutePosition {
        const float maxRemainingToPlanMore = 800;
        const float distanceToPlanAhead = 1500;

        public Route Route;
        public float Position;

        public RoutePosition(Route route, float position) {
            Route = route;
            Position = position;
        }

        public RoutePosition? Advance(float meters) {
            var position = Position + meters;
            var route = Route;

            //Check if a route needs popping
            var trimIdx = Route.Find(position);
            var elementsToPop = int.Clamp(trimIdx, 0, Route.LaneStrips.Length - 1);
            if(elementsToPop > 0) {
                var oldLength = route.Length();
                route = route.Pop(trimIdx);
                Debug.Assert(route != null, "Popped all elements");
                var newLength = route.Length();
                position -= oldLength - newLength;
            }

            //Check if a route needs to be planned
            if(route.Length() < maxRemainingToPlanMore) {
                route = route.Plan(distanceToPlanAhead);
            }

            //Check if the car is past the route
            if(position > route.Length()) {
                //Already past the route
                return null;
            }

            return new(route, position);
        }
    }
}
