using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Numerics;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds.Paths;

namespace TranSimCS.Cars {
    // Route planning and obstacle detection
    public partial class Car {
        public float RoutePositionFromStart;

        //Current lane placement, cached once per frame by CarStack so that the spatial
        //neighbour query in FindObstacle can compute arc-length distances to nearby cars
        //without scanning per-strip sorted lists.
        internal SplinePath? currentStrip;
        internal float currentStripPosition;
        internal bool currentStripIsReverse;

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
                countedLength += GetRouteElement(i).road.GetSpline().Length;
            if (countedLength > maxRemainingToPlanMore) return;
            while(countedLength < distanceToPlanAhead) {
                //Plan more segments
                var element = GetRouteElement(RouteElementCount - 1);
                if (element.road == null) {
                    log.Error($"The car {Guid} has an invalid route entry. Stopping route planning.");
                    break;
                }
                var candidates = FindNext(element.road, element.isReverse, SegmentHalf.End);
                if (candidates.Length == 0) {
                    break;
                }
                var next = candidates.GetRandomElement();
                countedLength += next.road.GetSpline().Length;
                PushRouteElement(next);
            }
        }

        public static RouteElement[] FindNext(SplinePath strip, bool isReverse, SegmentHalf half) {
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
            var result = nextLane.ConnectedLaneStrips.Select(FromEnd).ToArray();
            return result;
        }

        public bool Advance(float meters) {
            RoutePositionFromStart += meters;
            while (RouteElementCount > 0 && RoutePositionFromStart >= GetRouteElement(0).road.GetSpline().Length) {
                RoutePositionFromStart -= GetRouteElement(0).road.GetSpline().Length;
                PopRouteElements(1);
            }
                
            return RouteElementCount > 0;
        }
        public int FindIndexFromDistance(float meters) {
            float count = 0;
            for(int i = 0; i < RouteElementCount; ++i) {
                count += GetRouteElement(i).road.GetSpline().Length;
                if (count > meters) return i;
            }
            return RouteElementCount;
        }

        public Obstacle FindObstacle(float maxDist, float maxVelocity) {
            Obstacle obstacle = new(maxDist, maxVelocity);

            var minSegment = FindIndexFromDistance(RoutePositionFromStart);
            var maxSegment = FindIndexFromDistance(RoutePositionFromStart + maxDist);
            if (maxSegment >= RouteElementCount) maxSegment = RouteElementCount - 1;

            float count = 0;
            //Count distances until before the start segment
            for (int i = 0; i < minSegment; i++) count += GetRouteElement(i).road.GetSpline().Length;

            //Lookup tables built in the same pass as the traffic-light check. The spatial
            //neighbour query below uses them to classify each nearby car as either a queue
            //leader (on our own route) or a merge contender (on a sibling lane converging at
            //one of our route endpoints), replacing the former per-strip sorted-list scans.
            var routeSegMap = new Dictionary<SplinePath, (float startOffset, float length, bool isReverse)>();
            var mergeSiblingMap = new Dictionary<SplinePath, List<(SegmentHalf half, float sibLen, float ownDistanceToMerge)>>();

            for (int i = minSegment; i <= maxSegment; i++) {
                var key = GetRouteElement(i);
                var segment = key.road;
                var isReverse = key.isReverse;
                var endNode = isReverse ? segment.Start : segment.End;

                float segmentStartPosition = count;
                float segmentLength = key.road.GetSpline().Length;
                float segmentEndPosition = count + segmentLength;
                count = segmentEndPosition;

                float localPosition = RoutePositionFromStart - segmentStartPosition;

                //Remember this route segment so the spatial query can recognise cars on it
                //as queue leaders. The first (closest) occurrence wins for a looping route.
                if (!routeSegMap.ContainsKey(segment))
                    routeSegMap[segment] = (segmentStartPosition, segmentLength, isReverse);

                //Traffic lights are road state, not neighbours, so they stay position based.
                var isRed = endNode?.TrafficLight?.IsGreen(endNode) == false;
                if (isRed) {
                    Obstacle lightObstacle = new(segmentEndPosition - localPosition - 1, 0);
                    obstacle = obstacle.Combine(lightObstacle);
                }

                //Merge check: record every sibling lane converging at this endpoint. The
                //spatial query later finds cars on these siblings near the merge point.
                var rawSiblings = endNode?.ConnectedLaneStrips;
                var ownDistanceToMerge = segmentEndPosition - RoutePositionFromStart;
                if (rawSiblings != null && ownDistanceToMerge > 0) foreach (var sibling0 in rawSiblings) {
                    var path = sibling0.strip.Path;
                    if (path == segment) continue; //Do not check the same segment
                    if (!mergeSiblingMap.TryGetValue(path, out var list)) {
                        list = new();
                        mergeSiblingMap[path] = list;
                    }
                    list.Add((sibling0.half, path.GetSpline().Length, ownDistanceToMerge));
                }
            }

            //Spatial neighbour query. A single query around the car captures both queue
            //leaders (ahead on our route) and merge contenders (near a converging endpoint)
            //within maxDist, replacing the former per-strip sorted-list and sibling scans.
            var spatial = World?.Cars?.carSpatial;
            if (spatial != null) {
                var frame = GetPositionFrame();
                var pos = frame.O;
                float queryRadius = maxDist + 15f;
                var extent = new Vector3(queryRadius);
                var queryBox = new AABB(pos - extent, pos + extent);

                float bestLeaderDist = float.PositiveInfinity;
                float bestLeaderVel = maxVelocity;

                foreach (var cand in spatial.Query(queryBox)) {
                    if (ReferenceEquals(cand, this)) continue;
                    var cstrip = cand.currentStrip;
                    if (cstrip == null) continue;

                    if (routeSegMap.TryGetValue(cstrip, out var seg)) {
                        //Queueing: this car is ahead on our route.
                        float candCoord = seg.isReverse ? (seg.length - cand.currentStripPosition) : cand.currentStripPosition;
                        float candRoutePos = seg.startOffset + candCoord;
                        float distAhead = candRoutePos - RoutePositionFromStart;
                        if (distAhead > 0 && distAhead < bestLeaderDist) {
                            bestLeaderDist = distAhead;
                            bestLeaderVel = cand.Speed;
                            if (seg.isReverse) bestLeaderVel *= -1;
                        }
                    } else if (mergeSiblingMap.TryGetValue(cstrip, out var mergeList)) {
                        //Merging: this car is on a sibling lane converging at our endpoint.
                        for (int s = 0; s < mergeList.Count; s++) {
                            var mi = mergeList[s];
                            if (mi.ownDistanceToMerge <= 0) continue;
                            bool dirOk;
                            float contenderDistanceToMerge;
                            if (mi.half == SegmentHalf.End) {
                                contenderDistanceToMerge = mi.sibLen - cand.currentStripPosition;
                                dirOk = !cand.currentStripIsReverse;
                            } else {
                                contenderDistanceToMerge = cand.currentStripPosition;
                                dirOk = cand.currentStripIsReverse;
                            }
                            if (!dirOk) continue;
                            if (contenderDistanceToMerge > 10f || contenderDistanceToMerge <= 0) continue;

                            //A smaller distance means the sibling is further forward. GUID
                            //breaks exact ties so two cars never both enter the merge.
                            const float positionTieEpsilon = 0.001f;
                            var siblingHasPriority =
                                contenderDistanceToMerge < mi.ownDistanceToMerge - positionTieEpsilon ||
                                (MathF.Abs(contenderDistanceToMerge - mi.ownDistanceToMerge) <= positionTieEpsilon &&
                                 cand.Guid.CompareTo(Guid) < 0);
                            if (siblingHasPriority)
                                obstacle = obstacle.Combine(new Obstacle(mi.ownDistanceToMerge, 0));
                        }
                    }
                }

                if (bestLeaderDist < float.PositiveInfinity)
                    obstacle = obstacle.Combine(new Obstacle(bestLeaderDist - 5f, bestLeaderVel));
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

                var newDistance = distance - road.road.GetSpline().Length;
                if (newDistance >= 0) {
                    distance = newDistance;
                    continue;
                }

                const float eps = 0.001f;
                var currentOrthodistantLut = road.road.GetSpline();
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
