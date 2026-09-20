using System;
using System.Collections.Generic;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.SilkNet.RoadConstruction {
    /// <summary>
    /// Kind of transition for a single step of a <see cref="NodeSpec"/> alignment
    /// </summary>
    public enum LaneAlignmentKind {
        /// <summary>The start lane continues as the end lane</summary>
        Straight,
        /// <summary>Two start lanes merge into one end lane</summary>
        Merge,
        /// <summary>One start lane expands into two end lanes</summary>
        Expand,
        /// <summary>The start lane has no continuation on the end side</summary>
        Terminated,
        /// <summary>The end lane has no source on the start side</summary>
        Spawned
    }

    /// <summary>
    /// A single transition between a start <see cref="NodeSpec"/> and an end <see cref="NodeSpec"/>.
    /// Unspecified indices are set to -1.
    /// </summary>
    public readonly record struct LaneAlignmentStep(
        LaneAlignmentKind Kind,
        int StartIndex, int StartIndex2,
        int EndIndex, int EndIndex2) {
        public static LaneAlignmentStep Straight(int startIndex, int endIndex) =>
            new(LaneAlignmentKind.Straight, startIndex, -1, endIndex, -1);
        public static LaneAlignmentStep Merge(int startIndex, int startIndex2, int endIndex) =>
            new(LaneAlignmentKind.Merge, startIndex, startIndex2, endIndex, -1);
        public static LaneAlignmentStep Expand(int startIndex, int endIndex, int endIndex2) =>
            new(LaneAlignmentKind.Expand, startIndex, -1, endIndex, endIndex2);
        public static LaneAlignmentStep Terminate(int startIndex) =>
            new(LaneAlignmentKind.Terminated, startIndex, -1, -1, -1);
        public static LaneAlignmentStep Spawn(int endIndex) =>
            new(LaneAlignmentKind.Spawned, -1, -1, endIndex, -1);
    }

    /// <summary>
    /// Aligns two arbitrary lane lists into a set of lane transitions.
    /// Both lists must be ordered left to right in a common lateral frame.
    /// This supports any road combination (cars, rail, tram, pedestrian, ...) -
    /// lanes of different types are not mapped onto each other, and instead terminate or spawn.
    /// Lanes of the same kind can merge and expand at any position, not only at the outermost lanes.
    /// </summary>
    public static class NodeSpecAlignment {
        private const float Forbidden = -1e6f;
        private const float TransitionBonus = 0.4f;
        private const float SameTypeBonus = 0.5f;

        private const LaneFlags MergeFlagsMask =
            LaneFlags.MergeLeft | LaneFlags.MergeRight | LaneFlags.IsMerge | LaneFlags.NoLeft | LaneFlags.NoRight;

        private static bool Compatible(LaneNode s, LaneNode e) {
            var st = s.LaneSpec.VehicleTypes;
            var et = e.LaneSpec.VehicleTypes;
            if(st == VehicleTypes.None || et == VehicleTypes.None) return false;
            return (st & et) != 0;
        }

        private static float PairScore(LaneNode s, LaneNode e) {
            if(!Compatible(s, e)) return Forbidden;
            var width = MathF.Max(s.LaneSpec.Width, e.LaneSpec.Width);
            var widthSimilarity = 1 - MathF.Min(1, MathF.Abs(s.LaneSpec.Width - e.LaneSpec.Width) / width);
            var score = 1 + widthSimilarity;
            if(s.LaneSpec.VehicleTypes == e.LaneSpec.VehicleTypes) score += SameTypeBonus;
            return score;
        }

        private static float MergeScore(LaneNode s0, LaneNode s1, LaneNode e) {
            var a = PairScore(s0, e);
            var b = PairScore(s1, e);
            if(a <= Forbidden || b <= Forbidden) return Forbidden;
            return MathF.Max(a, b) + TransitionBonus;
        }

        private static float ExpandScore(LaneNode s, LaneNode e0, LaneNode e1) {
            var a = PairScore(s, e0);
            var b = PairScore(s, e1);
            if(a <= Forbidden || b <= Forbidden) return Forbidden;
            return MathF.Max(a, b) + TransitionBonus;
        }

        private enum Op : byte { None, Straight, Merge, Expand, Terminate, Spawn }

        /// <summary>
        /// Aligns the two lane lists. Both lists must be ordered left to right in a common lateral frame.
        /// The alignment preserves the lane order (no crossings), allows merges and expands anywhere,
        /// and terminates/spawns lanes where the types or counts do not match.
        /// </summary>
        public static List<LaneAlignmentStep> Align(IReadOnlyList<LaneNode> start, IReadOnlyList<LaneNode> end) {
            int n = start.Count, m = end.Count;
            var dp = new float[(n + 1) * (m + 1)];
            var choice = new Op[(n + 1) * (m + 1)];

            float D(int i, int j) => dp[i * (m + 1) + j];

            for(int i = n; i >= 0; i--) {
                for(int j = m; j >= 0; j--) {
                    if(i == n && j == m) {
                        dp[i * (m + 1) + j] = 0;
                        choice[i * (m + 1) + j] = Op.None;
                        continue;
                    }
                    //Candidates in priority order. Later candidates must strictly exceed to replace.
                    var best = float.NegativeInfinity;
                    var bestOp = Op.None;
                    if(i < n && j < m) {
                        var straight = PairScore(start[i], end[j]) + D(i + 1, j + 1);
                        if(straight > best) { best = straight; bestOp = Op.Straight; }
                        if(i + 1 < n) {
                            var merge = MergeScore(start[i], start[i + 1], end[j]) + D(i + 2, j + 1);
                            if(merge > best) { best = merge; bestOp = Op.Merge; }
                        }
                        if(j + 1 < m) {
                            var expand = ExpandScore(start[i], end[j], end[j + 1]) + D(i + 1, j + 2);
                            if(expand > best) { best = expand; bestOp = Op.Expand; }
                        }
                    }
                    if(i < n) {
                        var terminate = D(i + 1, j);
                        if(terminate > best) { best = terminate; bestOp = Op.Terminate; }
                    }
                    if(j < m) {
                        var spawn = D(i, j + 1);
                        if(spawn > best) { best = spawn; bestOp = Op.Spawn; }
                    }
                    dp[i * (m + 1) + j] = best;
                    choice[i * (m + 1) + j] = bestOp;
                }
            }

            //Backtrack
            var steps = new List<LaneAlignmentStep>();
            int ci = 0, cj = 0;
            while(ci < n || cj < m) {
                switch(choice[ci * (m + 1) + cj]) {
                    case Op.Straight:
                        steps.Add(LaneAlignmentStep.Straight(ci, cj));
                        ci++; cj++;
                        break;
                    case Op.Merge:
                        steps.Add(LaneAlignmentStep.Merge(ci, ci + 1, cj));
                        ci += 2; cj++;
                        break;
                    case Op.Expand:
                        steps.Add(LaneAlignmentStep.Expand(ci, cj, cj + 1));
                        ci++; cj += 2;
                        break;
                    case Op.Terminate:
                        steps.Add(LaneAlignmentStep.Terminate(ci));
                        ci++;
                        break;
                    case Op.Spawn:
                        steps.Add(LaneAlignmentStep.Spawn(cj));
                        cj++;
                        break;
                    default:
                        //No lanes left to consume
                        return steps;
                }
            }
            return steps;
        }

        /// <summary>
        /// Converts alignment steps into <see cref="LaneMapping"/>s, following the merge/expand flag
        /// conventions of <see cref="LaneMappings"/>. Terminated and spawned lanes produce no mapping.
        /// </summary>
        public static List<LaneMapping> ToLaneMappings(IReadOnlyList<LaneAlignmentStep> steps,
            IReadOnlyList<LaneNode> start, IReadOnlyList<LaneNode> end) {
            var result = new List<LaneMapping>();
            foreach(var step in steps) {
                switch(step.Kind) {
                    case LaneAlignmentKind.Straight: {
                        var source = start[step.StartIndex];
                        result.Add(new LaneMapping(step.StartIndex, step.EndIndex, source.LaneSpec, source.ID));
                        break;
                    }
                    case LaneAlignmentKind.Merge: {
                        var a = start[step.StartIndex];
                        var b = start[step.StartIndex2];
                        var target = end[step.EndIndex];
                        //The source farther away from the target is the outer one and carries the merge flag
                        var outer = MathF.Abs(a.CenterPos - target.CenterPos) >= MathF.Abs(b.CenterPos - target.CenterPos) ? a : b;
                        var inner = ReferenceEquals(outer, a) ? b : a;
                        var direction = target.CenterPos >= outer.CenterPos ? LaneFlags.MergeRight : LaneFlags.MergeLeft;
                        var outerSpec = outer.LaneSpec;
                        outerSpec.Flags |= LaneFlags.IsMerge | direction;
                        result.Add(new LaneMapping(IndexOf(start, outer), step.EndIndex, outerSpec, outer.ID));
                        var innerSpec = inner.LaneSpec;
                        innerSpec.Flags |= LaneFlags.MergeLeft | LaneFlags.MergeRight;
                        result.Add(new LaneMapping(IndexOf(start, inner), step.EndIndex, innerSpec, inner.ID));
                        break;
                    }
                    case LaneAlignmentKind.Expand: {
                        var source = start[step.StartIndex];
                        var endA = end[step.EndIndex];
                        var endB = end[step.EndIndex2];
                        //The end lane closer in width is the passthrough, the other one is spawned
                        var widthA = MathF.Abs(endA.LaneSpec.Width - source.LaneSpec.Width);
                        var widthB = MathF.Abs(endB.LaneSpec.Width - source.LaneSpec.Width);
                        var passthrough = widthA <= widthB ? endA : endB;
                        var spawned = ReferenceEquals(passthrough, endA) ? endB : endA;

                        var passSpec = source.LaneSpec;
                        passSpec.Flags &= ~MergeFlagsMask;
                        result.Add(new LaneMapping(step.StartIndex, IndexOf(end, passthrough), passSpec, source.ID));

                        var spawnSpec = source.LaneSpec;
                        spawnSpec.Flags |= spawned.CenterPos >= passthrough.CenterPos ? LaneFlags.MergeRight : LaneFlags.MergeLeft;
                        result.Add(new LaneMapping(step.StartIndex, IndexOf(end, spawned), spawnSpec, source.ID));
                        break;
                    }
                }
            }
            return result;
        }

        private static int IndexOf(IReadOnlyList<LaneNode> lanes, LaneNode lane) {
            for(int i = 0; i < lanes.Count; i++)
                if(ReferenceEquals(lanes[i], lane)) return i;
            return -1;
        }
    }
}
