using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.ClassInstances;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;

namespace TranSimCS.Tools.RoadConstruction {
    public record struct LaneMappingEdge(LaneNode Destination, LaneSpec LaneSpec) {}
    public struct LaneMappingOutput {
        public LaneMappingInput Source;
        public ImmutableArray<LaneMappingEdge> GeneratedLanes;
        public LaneNode? PassThrough;
    }
    public static class LaneMappingsGenerator {
        public static LaneMappingOutput[] Generate(LaneMappingInputs inputs) {
            var tmp = new LaneMappingOutput?[inputs.LaneMappings.Length];
            var isProcessing = new bool[inputs.LaneMappings.Length];
            var results = new LaneMappingOutput[inputs.LaneMappings.Length];

            LaneNode FindLeftMergeTarget(int index) {
                var prevIndex = index - 1;
                if (prevIndex < 0) throw new LaneValidationException("Merging into the left bound");
                var prevLane = inputs.LaneMappings[prevIndex];
                if (prevLane.IsMergeIntoLeftOrEnd && prevLane.IsMergeIntoRightOrEnd) return FindLeftMergeTarget(prevIndex);
                if (prevLane.IsMergeIntoRightOrEnd) throw new LaneValidationException("Lanes merge into each other");
                if (prevLane.IsMergeIntoLeftOrEnd) return FindLeftMergeTarget(prevIndex);
                var prevDependency = Resolve(prevIndex);
                return prevDependency.GeneratedLanes[^1].Destination;
            }
            LaneNode FindRightMergeTarget(int index) {
                var nextIndex = index + 1;
                if (nextIndex >= inputs.LaneMappings.Length) throw new LaneValidationException("Merging into the right bound");
                var nextLane = inputs.LaneMappings[nextIndex];
                if (nextLane.IsMergeIntoLeftOrEnd && nextLane.IsMergeIntoRightOrEnd) return FindRightMergeTarget(nextIndex);
                if (nextLane.IsMergeIntoLeftOrEnd) throw new LaneValidationException("Lanes merge into each other");
                if (nextLane.IsMergeIntoRightOrEnd) return FindRightMergeTarget(nextIndex);
                var nextDependency = Resolve(nextIndex);
                return nextDependency.GeneratedLanes[0].Destination;
            }
            LaneMappingOutput Resolve(int index) {
                //Check caches
                var cachedValue = tmp[index];
                if (cachedValue != null) return cachedValue.Value;

                //Lock the index for cycle detection
                if (isProcessing[index]) 
                    throw new LaneValidationException("Lane cycle detected");
                isProcessing[index] = true;

                var mappedLane = inputs.LaneMappings[index];
                var classification = mappedLane.Classify();

                //Split by case
                LaneMappingOutput result = default;
                result.GeneratedLanes = ImmutableArray<LaneMappingEdge>.Empty;
                result.Source = mappedLane;

                var noLines = LaneFlags.MergeLeft | LaneFlags.MergeRight;
                var mergeFlagsMask = noLines | LaneFlags.IsMerge;

                switch (classification) {
                    case LaneMappingInputClassification.End:
                        //The lane terminates
                        break;
                    case LaneMappingInputClassification.MergeLeft:
                        //Merge towards the left
                        var leftDestNode = FindLeftMergeTarget(index);
                        var lanespec = leftDestNode.LaneSpec;
                        lanespec.Flags = lanespec.Flags & ~LaneFlags.MergeRight | LaneFlags.MergeLeft | LaneFlags.IsMerge;
                        int nextIndex = index + 1;
                        if (nextIndex < inputs.LaneMappings.Length && inputs.LaneMappings[nextIndex].IsMergeIntoLeft)
                            lanespec.Flags |= LaneFlags.MergeRight;
                        var edge = new LaneMappingEdge(leftDestNode, lanespec);
                        result.GeneratedLanes = ImmutableArray.Create(edge);
                        result.PassThrough = leftDestNode;
                        break;
                    case LaneMappingInputClassification.MergeRight:
                        //Merge towards the right
                        var rightDestNode = FindRightMergeTarget(index);
                        var lanespec2 = rightDestNode.LaneSpec;
                        lanespec2.Flags = lanespec2.Flags & ~LaneFlags.MergeLeft | LaneFlags.MergeRight | LaneFlags.IsMerge;
                        int prevIndex = index - 1;
                        if(prevIndex >= 0 && inputs.LaneMappings[prevIndex].IsMergeIntoRight)
                            lanespec2.Flags |= LaneFlags.MergeLeft;
                        var edge2 = new LaneMappingEdge(rightDestNode, lanespec2);
                        result.GeneratedLanes = ImmutableArray.Create(edge2);
                        result.PassThrough = rightDestNode;
                        break;
                    default:
                        //Construct lanes
                        var CenterOffset = mappedLane.LeftAmount.Amount;
                        var initialX = mappedLane.SourceLane.MiddlePosition - CenterOffset * mappedLane.SourceLane.Width;
                        var laneCount = mappedLane.LeftAmount.Amount + mappedLane.RightAmount.Amount + 1;
                        var laneNodes = new LaneMappingEdge[laneCount];
                        for (int i = 0; i < laneCount; i++) {
                            var x = initialX + i * mappedLane.SourceLane.Width;
                            var offsetFromCenter = i - mappedLane.LeftAmount.Amount;
                            var spec = mappedLane.SourceLane.Spec;

                            //Classify into 5 regions
                            
                            Span<LaneFlags> laneFlagsSpan = [LaneFlags.MergeLeft, noLines, LaneFlags.None, noLines, LaneFlags.MergeRight];
                            var appliedFlagIndex =
                                (i == 0) ? 0 :
                                (i == laneCount - 1) ? 4 :
                                (offsetFromCenter == 0) ? 2 :
                                (offsetFromCenter < 0) ? 1 : 3;
                            var appliedFlags = laneFlagsSpan[appliedFlagIndex];
                            

                            spec.Flags = spec.Flags.SetFieldTo(mergeFlagsMask, appliedFlags);

                            var laneNode3 = new LaneNode(spec, x);
                            laneNodes[i] = new(laneNode3, spec);
                        }

                        result.GeneratedLanes = laneNodes.ToImmutableArray();
                        result.Source = mappedLane;
                        result.PassThrough = result.GeneratedLanes[CenterOffset].Destination;
                        break;
                }

                tmp[index] = result;
                isProcessing[index] = false;
                return result;
            }

            for (int i = 0; i < inputs.LaneMappings.Length; i++) {
                results[i] = Resolve(i);
            }

            //Reposition lanes
            Reposition(results);
            
            return results;
        }

        class LaneMappingNode {
            public int originalIndex;
            public LaneMappingOutput group;
            public Range<float> originalBounds;
            public float leftOffset;
            public float rightOffset;
            public LaneMappingNode(LaneMappingOutput group, int originalIndex) {
                Debug.Assert(group.GeneratedLanes.Length >= 0, "No lanes to union"); 
                this.group = group;
                this.originalBounds = group.GeneratedLanes.Select(x => x.Destination.Bounds).RangeUnion();
                this.originalIndex = originalIndex;
            }
        }
        private static void Reposition(LaneMappingOutput[] outputs) {
            //Generate repositioning nodes
            var repositionNodes = outputs.Select((x, i) => new LaneMappingNode(x, i)).ToArray();

            if(repositionNodes.Length <= 1) return;

            //Right pass
            for(int i = 1; i < repositionNodes.Length; i++) {
                var prev = repositionNodes[i - 1];
                var next = repositionNodes[i];
                var overlap = (prev.rightOffset + prev.originalBounds.Max) - (next.rightOffset + next.originalBounds.Min);
                if(overlap > 0) next.rightOffset = overlap;
            }

            //Left pass
            for (int i = (repositionNodes.Length - 1); i >= 1; i--) {
                var prev = repositionNodes[i];
                var next = repositionNodes[i - 1];
                var overlap = (prev.leftOffset + prev.originalBounds.Min) - (next.leftOffset + next.originalBounds.Max);
                if (overlap < 0) next.leftOffset = overlap;
            }

            //Average offsets + Generate replacements
            Dictionary<Guid, LaneNode> replacements = new Dictionary<Guid, LaneNode>();
            foreach (var group in repositionNodes) {
                var offset = (group.leftOffset + group.rightOffset) / 2;
                foreach(var lane in group.group.GeneratedLanes) {
                    var guid = lane.Destination.ID;
                    var laneSpec = lane.Destination.LaneSpec;
                    var newPosition = lane.Destination.CenterPos + offset;
                    var replacement = new LaneNode(laneSpec, newPosition, guid);
                    replacements.Add(guid, replacement);
                }
            }

            LaneNode? Replace(LaneNode? source) {
                if (source == null) return null;
                if(replacements.TryGetValue(source.ID, out var replacement)) return replacement;
                return source;
            }
            LaneMappingEdge ReplaceEdge(LaneMappingEdge edge) => new(Replace(edge.Destination)!, edge.LaneSpec);

            //Apply replacements
            for(int i = 0; i < outputs.Length; i++) {
                var node = outputs[i];
                node.PassThrough = Replace(node.PassThrough);
                node.GeneratedLanes = node.GeneratedLanes.Select(ReplaceEdge).ToImmutableArray();
                outputs[i] = node;
            }
        }
    }
}
