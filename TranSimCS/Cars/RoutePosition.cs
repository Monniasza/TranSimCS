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

        public RoutePosition? Trim() {
            var trimmed = Route.Trim();
            if (trimmed == null) return null;
            return new(trimmed, Position);
        }

        public RoutePosition PlanIfNeeded(float meters) {
            var position = Position + meters;
            var route = Route;
            if (route.Length() < maxRemainingToPlanMore) {
                route = route.Plan(distanceToPlanAhead);
            }
            return new(route, position);
        }

        public RoutePosition? Advance(float meters) {
            var position = Position + meters;
            var route = Route;

            //Check if a route needs popping
            var trimIdx = Route.Find(position);
            var elementsToPop = int.Clamp(trimIdx, 0, Route.LaneStrips.Length - 1);
            if(elementsToPop > 0) {
                var oldLength = route.Length();
                route = route.Pop(elementsToPop);
                Debug.Assert(route != null, "Popped all elements");
                var newLength = route.Length();
                position -= oldLength - newLength;
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
            if (maxSegment >= Route.LaneStrips.Length) maxSegment = Route.LaneStrips.Length - 1;
            for (int i = minSegment; i <= maxSegment; i++) {
                bool earlyExit = false;
                var key = Route.LaneStrips[i];
                var segment = key.road;
                var isReverse = key.isReverse;
                var endNode = isReverse ? segment.StartLane : segment.EndLane;
                float localPosition = Position - key.StartPosition;

                //Find the next car ahead
                int nextCarAheadIndex = 0;
                if (isReverse) 
                    nextCarAheadIndex = segment.FindLastBehindIndex(key.Span - localPosition);
                 else 
                    nextCarAheadIndex = segment.FindFirstAheadIndex(localPosition);
                if(nextCarAheadIndex >= 0 && nextCarAheadIndex < segment.CarsOnStrip.Count) {
                    //A car was found
                    var nextCar = segment.CarsOnStrip[nextCarAheadIndex];
                    var carPosition = nextCar.positionOnStrip;
                    if(isReverse) carPosition = key.Span - carPosition;
                    var velocity = nextCar.car.Speed;
                    if (isReverse) velocity *= -1;
                    Obstacle carObstacle = new(carPosition - localPosition - 3, velocity);
                    obstacle = obstacle.Combine(carObstacle);
                    earlyExit = true;
                }

                var attachedTrafficLight = endNode.TrafficLight;
                if (attachedTrafficLight == null) continue;
                var isGreen = attachedTrafficLight.IsGreen(endNode);
                if (!isGreen) {
                    Obstacle lightObstacle = new(key.EndPosition - localPosition - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                    earlyExit = true; //Any further traffic lights will be obscured
                }

                if (earlyExit) break;
            }

            return obstacle;
        }

        public Transform3 GetPositionFrame() => Route.GetPosition(Position);
    }
}
