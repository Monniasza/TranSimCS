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
        public Obstacle FindObstacle(float maxDist, float maxVelocity, Car self) {
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

                //Merge check: the car furthest forward gets priority.  Without this,
                //two cars near the merge both yield and deadlock.
                var rawSiblings = endNode.ConnectedLaneStrips;
                var distanceToMerge = key.EndPosition - localPosition - 10;
                Obstacle mergeObstacle = new Obstacle(distanceToMerge, 0);
                var ownDistanceToMerge = key.Span - localPosition;
                foreach(var sibling in rawSiblings) {
                    var cars = sibling.strip._carsOnStrip;
                    var length = sibling.strip.SplineLUT.Length;

                    if (sibling.strip == segment) continue; //Do not check the same segment
                    if (cars.Count == 0) continue; //No cars on the sibling

                    CarEntry? contender = null;
                    float contenderDistanceToMerge = 0;

                    if (sibling.half == SegmentHalf.End) {
                        //Entries are sorted by position, so the last forward car is
                        //the one closest to this endpoint.
                        for (int j = cars.Count - 1; j >= 0; j--) {
                            var car = cars[j];
                            contenderDistanceToMerge = length - car.positionOnStrip;
                            if (contenderDistanceToMerge > 10)
                                break;

                            if (!car.isReverse) {
                                contender = car;
                                break;
                            }
                        }
                    } else {
                        //The first reverse car is closest to this endpoint.
                        for (int j = 0; j < cars.Count; j++) {
                            var car = cars[j];
                            contenderDistanceToMerge = car.positionOnStrip;
                            if (contenderDistanceToMerge > 10)
                                break;

                            if (car.isReverse) {
                                contender = car;
                                break;
                            }
                        }
                    }

                    if (contender == null) continue;

                    // A smaller distance means the sibling is further forward. GUID
                    // breaks exact ties so two cars never both enter the merge.
                    const float positionTieEpsilon = 0.001f;
                    var siblingHasPriority =
                        contenderDistanceToMerge < ownDistanceToMerge - positionTieEpsilon ||
                        (MathF.Abs(contenderDistanceToMerge - ownDistanceToMerge) <= positionTieEpsilon &&
                         contender.Value.car.Guid.CompareTo(self.Guid) < 0);
                    if (siblingHasPriority)
                        obstacle = obstacle.Combine(mergeObstacle);
                }
            }
            return obstacle;
        }

        public Transform3 GetPositionFrame() => Route.GetPosition(Position);
    }
}
