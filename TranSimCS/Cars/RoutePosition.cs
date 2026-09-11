using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Vulkan;
using TranSimCS.Geometry;

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
        public Obstacle FindObstacle(float maxDist, float maxVelocity) {
            Obstacle obstacle = new(maxDist, maxVelocity);

            //Find traffic lights
            var minSegment = Route.Find(Position);
            var maxSegment = Route.Find(Position + maxDist);
            for (int i = minSegment; i <= maxSegment; i++) {
                var key = Route.LaneStrips[i];
                var segment = key.road;
                var isReverse = key.isReverse;
                var endNode = isReverse ? segment.StartLane : segment.EndLane;
                var attachedTrafficLight = endNode.TrafficLight;
                if (attachedTrafficLight == null) continue;
                var isGreen = attachedTrafficLight.IsGreen(endNode);
                if (!isGreen) {
                    Obstacle lightObstacle = new(key.EndPosition - Position - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                    break; //Any further traffic lights will be obscured
                }
            }

            return obstacle;
        }

        public Transform3 GetPositionFrame() => Route.GetPosition(Position);
    }
}
