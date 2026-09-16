using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Cars {
    // Route planning and obstacle detection
    public partial class Car {
        public float RoutePositionFromStart;
        internal bool TrimUntilDead() {
            int i;
            for(i = 0; i < RouteElementCount; i++) {
                var strip = GetRouteElement(i).road;
                if (strip?.Road == null) break;
            }
            if(i == 0) {
                //The car is dead
                return true;
            }

            TrimRouteElements(RouteElementCount - i);
            return false;
        }
        internal void PlanAhead() {
            const float maxRemainingToPlanMore = 800;
            const float distanceToPlanAhead = 1500;
            float countedLength = 0;
            for(int i = 0; i < RouteElementCount; i++) 
                countedLength += GetRouteElement(i).road.SplineLUT.Length;
            if (countedLength > maxRemainingToPlanMore) return;
            while(countedLength < distanceToPlanAhead) {
                //Plan more segments
                var element = GetRouteElement(RouteElementCount - 1);
                var candidates = RouteMethods.FindNext(element.road, element.isReverse, SegmentHalf.End).ToArray();
                if (candidates.Length == 0) {
                    break;
                }
                var next = candidates.GetRandomElement();
                countedLength += next.LaneStrip.SplineLUT.Length;
                PushRouteElement(new(next.LaneStrip, next.IsReverse));
            }
        }
        public bool Advance(float meters) {
            RoutePositionFromStart += meters;
            while (RouteElementCount > 0 && RoutePositionFromStart >= GetRouteElement(0).road.SplineLUT.Length) {
                RoutePositionFromStart -= GetRouteElement(0).road.SplineLUT.Length;
                PopRouteElements(1);
            }
                
            return RouteElementCount > 0;
        }
        public int FindIndexFromDistance(float meters) {
            float count = 0;
            for(int i = 0; i < RouteElementCount; ++i) {
                count += GetRouteElement(i).road.SplineLUT.Length;
                if (count > meters) return i;
            }
            return RouteElementCount;
        }

        public Obstacle FindObstacle(float maxDist, float maxVelocity) {
            Obstacle obstacle = new(maxDist, maxVelocity);

            //Find traffic lights
            var minSegment = FindIndexFromDistance(RoutePositionFromStart);
            var maxSegment = FindIndexFromDistance(RoutePositionFromStart + maxDist);
            if (maxSegment >= RouteElementCount) maxSegment = RouteElementCount - 1;

            float count = 0;
            //Count distances until before the start segment
            for (int i = 0; i < minSegment; i++) count += GetRouteElement(i).road.SplineLUT.Length;

            for (int i = minSegment; i <= maxSegment; i++) {
                var key = GetRouteElement(i);
                var segment = key.road;
                var isReverse = key.isReverse;
                var endNode = isReverse ? segment.StartLane : segment.EndLane;

                float segmentStartPosition = count;
                float segmentLength = key.road.SplineLUT.Length;
                float segmentEndPosition = count + segmentLength;
                count = segmentEndPosition;

                float localPosition = RoutePositionFromStart - segmentStartPosition;

                //Find the next car ahead
                int nextCarAheadIndex = 0;
                if (isReverse)
                    nextCarAheadIndex = segment.FindLastBehindIndex(segmentLength - localPosition);
                else
                    nextCarAheadIndex = segment.FindFirstAheadIndex(localPosition);
                if (nextCarAheadIndex >= 0 && nextCarAheadIndex < segment.CarsOnStrip.Count) {
                    //A car was found
                    var nextCar = segment.CarsOnStrip[nextCarAheadIndex];
                    var carPosition = nextCar.positionOnStrip;
                    if (isReverse) carPosition = segmentLength - carPosition;
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
                    Obstacle lightObstacle = new(segmentEndPosition - localPosition - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                }

                //Merge check: the car furthest forward gets priority.  Without this,
                //two cars near the merge both yield and deadlock.
                var rawSiblings = endNode.ConnectedLaneStrips;
                var distanceToMerge = segmentEndPosition - localPosition - 10;
                Obstacle mergeObstacle = new Obstacle(distanceToMerge, 0);
                var ownDistanceToMerge = segmentLength - localPosition;
                foreach (var sibling in rawSiblings) {
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
                         contender.Value.car.Guid.CompareTo(Guid) < 0);
                    if (siblingHasPriority)
                        obstacle = obstacle.Combine(mergeObstacle);
                }
            }
            return obstacle;
        }

        public Transform3 GetPositionFrame(){
            var distance = RoutePositionFromStart;
            for (int i = 0; i < RouteElementCount; i++) {
                var road = GetRouteElement(i);
                var newDistance = distance - road.road.SplineLUT.Length;
                if (newDistance >= 0) {
                    distance = newDistance;
                    continue;
                }

                const float eps = 0.001f;
                var currentStrip = road.ToCarStripPosition(distance);
                var positionLUT = currentStrip.GetPositionLookup();
                var prevXYZT = positionLUT[distance];
                var t = prevXYZT.W;
                var resample = currentStrip.GetPositionFrame(t);

                //Validation
                Debug.Assert(float.IsFinite(t), "Invalid spline parameter");
                Debug.Assert(resample.O.IsFinite(), "Invalid position");
                Debug.Assert(resample.X.IsFinite(), "Invalid lateral");
                Debug.Assert(resample.Y.IsFinite(), "Invalid normal");
                Debug.Assert(resample.Z.IsFinite(), "Invalid tangent");
                return resample;
            }

            Debug.Fail("Car is beyond the end, but not removed");
            throw null;
        }
    }
}
