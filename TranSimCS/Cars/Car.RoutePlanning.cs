using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Cars {
    // Route planning and obstacle detection
    public partial class Car {
        public float RoutePositionFromStart;
        internal bool TrimUntilDead() {
            int i;
            for(i = 0; i < RouteElementCount; i++) {
                var strip = GetRouteElement(i).road;
                if (strip == null) break;
                if (i > 0 && strip.CurrentState == Worlds.Paths.PathState.Orphaned) break;
                if(strip.CurrentState == Worlds.Paths.PathState.Deleted) {
                    if (i == 0) Debug.Fail("Path got deleted from under the car");
                    break;
                }
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
                countedLength += GetRouteElement(i).road.LUT.Length;
            if (countedLength > maxRemainingToPlanMore) return;
            while(countedLength < distanceToPlanAhead) {
                //Plan more segments
                var element = GetRouteElement(RouteElementCount - 1);
                if (element.road == null) {
                    log.Error($"The car {Guid} has an invalid route entry. Stopping route planning.");
                    break;
                }
                var candidates = FindNext(element.road, element.isReverse, SegmentHalf.End).ToArray();
                if (candidates.Length == 0) {
                    break;
                }
                var next = candidates.GetRandomElement();
                countedLength += next.road.LUT.Length;
                PushRouteElement(next);
            }
        }

        public static IEnumerable<RouteElement> FindNext(SplinePath strip, bool isReverse, SegmentHalf half) {
            static RouteElement FromEnd(LaneStripEnd laneStrip) {
                var isEntryFromEnd = laneStrip.half == SegmentHalf.End;
                return new(laneStrip.strip.Path, isEntryFromEnd);
            }

            ArgumentNullException.ThrowIfNull(strip);
            if (isReverse) half = half.Inverse();
            var nextLane = half.GetConditional(strip.Start, strip.End);
            if (nextLane == null) return [];
            nextLane = nextLane.OppositeHalf;
            if (nextLane == null) return [];
            return nextLane.ConnectedLaneStrips.Select(FromEnd);
        }

        public bool Advance(float meters) {
            RoutePositionFromStart += meters;
            while (RouteElementCount > 0 && RoutePositionFromStart >= GetRouteElement(0).road.LUT.Length) {
                RoutePositionFromStart -= GetRouteElement(0).road.LUT.Length;
                PopRouteElements(1);
            }
                
            return RouteElementCount > 0;
        }
        public int FindIndexFromDistance(float meters) {
            float count = 0;
            for(int i = 0; i < RouteElementCount; ++i) {
                count += GetRouteElement(i).road.LUT.Length;
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
            for (int i = 0; i < minSegment; i++) count += GetRouteElement(i).road.LUT.Length;

            for (int i = minSegment; i <= maxSegment; i++) {
                var key = GetRouteElement(i);
                var segment = key.road;
                var isReverse = key.isReverse;
                var endNode = isReverse ? segment.Start : segment.End;

                float segmentStartPosition = count;
                float segmentLength = key.road.LUT.Length;
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

                var isRed = endNode?.TrafficLight?.IsGreen(endNode) == false;
                if (isRed) {
                    Obstacle lightObstacle = new(segmentEndPosition - localPosition - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                }

                //Merge check: the car furthest forward gets priority.  Without this,
                //two cars near the merge both yield and deadlock.
                var rawSiblings = endNode?.ConnectedLaneStrips;
                var distanceToMerge = segmentLength - localPosition;
                Obstacle mergeObstacle = new Obstacle(distanceToMerge, 0);
                var ownDistanceToMerge = distanceToMerge;
                if(rawSiblings != null && ownDistanceToMerge > 0) foreach (var sibling0 in rawSiblings) {
                    var path = sibling0.strip.Path;
                    var half = sibling0.half;

                    var cars = path._carsOnStrip;
                    var length = path.LUT.Length;

                    if (path == segment) continue; //Do not check the same segment
                    if (cars.Count == 0) continue; //No cars on the sibling

                    CarEntry? contender = null;
                    float contenderDistanceToMerge = 0;

                    if (half == SegmentHalf.End) {
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

        /// <summary>
        /// Computes the world-space reference frame of this car at its current position along its route.
        /// <para>
        /// This is robust against a segment being deleted from under the car without the car being
        /// notified: a route element whose strip has died is skipped rather than dereferenced. If no
        /// live element can be found, the car's last known frame is returned instead of throwing, so
        /// that a deletion can never crash the simulation.
        /// </para>
        /// </summary>
        /// <returns>The reference frame of the car.</returns>
        public Transform3 GetPositionFrame(){
            var distance = RoutePositionFromStart;
            for (int i = 0; i < RouteElementCount; i++) {
                var road = GetRouteElement(i);

                //The strip may have been deleted from under us. Skip it instead of dereferencing a
                //dead strip, which would throw a NullReferenceException.
                if (road.road == null) continue;
                Debug.Assert(road.road.CurrentState != PathState.Deleted);

                var newDistance = distance - road.road.LUT.Length;
                if (newDistance >= 0) {
                    distance = newDistance;
                    continue;
                }

                const float eps = 0.001f;
                var currentOrthodistantLut = road.road.LUT;
                var positionLUT = road.isReverse ? currentOrthodistantLut.Reverse : currentOrthodistantLut.Forward;
                var prevXYZT = positionLUT[distance];
                var t = prevXYZT.W;
                var resample = currentOrthodistantLut.spline.SampleFrame(t);

                //Validation
                Debug.Assert(float.IsFinite(t), "Invalid spline parameter");
                Debug.Assert(resample.O.IsFinite(), "Invalid position");
                Debug.Assert(resample.X.IsFinite(), "Invalid lateral");
                Debug.Assert(resample.Y.IsFinite(), "Invalid normal");
                Debug.Assert(resample.Z.IsFinite(), "Invalid tangent");
                return resample;
            }

            //No live route element could be found. This happens when the whole route was deleted from
            //under the car. Fall back to the last known frame rather than throwing, so that the car
            //survives until the next update trims or removes it.
            log.Warn($"The car {Guid} has no live route element to position against. Keeping the last known frame.");
            return new Transform3(meshInstance.Transform.ToMatrix());
        }
    }
}
