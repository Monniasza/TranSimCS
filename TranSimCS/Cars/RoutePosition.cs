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
            var position = Position;
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
                    if (isReverse) carPosition = key.Span - carPosition;
                    var velocity = nextCar.car.Speed;
                    if (isReverse) velocity *= -1;

                    //Validate the lookup
                    var distToVehicle = carPosition - localPosition;
                    var distanceToObstacle = distToVehicle - 5;
                    
                    Obstacle carObstacle = new(distanceToObstacle, velocity);
                    obstacle = obstacle.Combine(carObstacle);
                }

                var attachedTrafficLight = endNode.TrafficLight;
                var isGreen = attachedTrafficLight == null || attachedTrafficLight.IsGreen(endNode);
                if (!isGreen) {
                    Obstacle lightObstacle = new(key.EndPosition - localPosition - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                }

                //Merge check: Do not merge if cars are 10 m or less behind
                var rawSiblings = endNode.ConnectedLaneStrips;
                var distanceToMerge = key.EndPosition - localPosition - 10;
                Obstacle mergeObstacle = new Obstacle(distanceToMerge, 0);
                foreach(var sibling in rawSiblings) {

                    //Check each sibling
                    var cars = sibling.strip._carsOnStrip;
                    var length = sibling.strip.SplineLUT.Length;

                    if (sibling.strip == segment) continue; //Do not check the same segment
                    if (sibling.strip._carsOnStrip.Count == 0) continue; //No cars on the sibling

                    if (sibling.half == SegmentHalf.End) {
                        //Going forward
                        for (int j = cars.Count - 1; j >= 0; j--) {
                            var car = cars[j];

                            // Cars farther than 10 m from the endpoint can be ignored.
                            if (length - car.positionOnStrip > 10)
                                break;

                            if (!car.isReverse) {
                                obstacle = obstacle.Combine(mergeObstacle);
                                break;
                            }
                        }
                    } else {
                        //Going backward
                        for (int j = 0; j < cars.Count; j++) {
                            var car = cars[j];

                            // Cars farther than 10 m from the endpoint can be ignored.
                            if (car.positionOnStrip > 10)
                                break;

                            if (car.isReverse) {
                                obstacle = obstacle.Combine(mergeObstacle);
                                break;
                            }
                        }
                    }
                }
            }
            return obstacle;
        }

        public Transform3 GetPositionFrame() => Route.GetPosition(Position);
    }
}
