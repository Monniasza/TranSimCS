using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using TranSimCS.Roads.Node;

namespace TranSimCS.Tools.RoadConstruction {
    /// <summary>
    /// A <see cref="LaneMappingInput"/> describes the change in 
    /// </summary>
    public struct LaneMappingInput {
        /// The lane that is placed on the beginning of this operation
        public Lane SourceLane { get; }
        /// How much does the left side shift? <see cref="MergeAmount.Merge"> for a merge.
        public MergeAmount LeftAmount { get; }
        /// How much does the right side shift? <see cref="MergeAmount.Merge"> for a merge.
        public MergeAmount RightAmount { get; }

        public LaneMappingInput(Lane sourceLane, MergeAmount leftAmount, MergeAmount rightAmount) {
            if (leftAmount.IsMerge && rightAmount.IsExpand)
                throw new LaneValidationException("A lane cannot both expand and merge into right");
            if (rightAmount.IsMerge && leftAmount.IsExpand)
                throw new LaneValidationException("A lane cannot both expand and merge into left");

            SourceLane = sourceLane;
            LeftAmount = leftAmount;
            RightAmount = rightAmount;
        }

        /// <summary>
        /// Classifies this <see cref="LaneMappingInputs"/> into one of 7 possible categories
        /// </summary>
        public LaneMappingInputClassification Classify() {
            if (LeftAmount.IsStraight) {
                if (RightAmount.IsStraight) return LaneMappingInputClassification.Straight;
                if (RightAmount.IsExpand) return LaneMappingInputClassification.ExitRight;
                if (RightAmount.IsMerge) return LaneMappingInputClassification.MergeLeft;
            }
            if (LeftAmount.IsExpand) {
                if (RightAmount.IsExpand) return LaneMappingInputClassification.ExitLeftRight;
                if (RightAmount.IsStraight) return LaneMappingInputClassification.ExitLeft;
            }
            if (LeftAmount.IsMerge) {
                if (RightAmount.IsMerge) return LaneMappingInputClassification.End;
                if (RightAmount.IsStraight) return LaneMappingInputClassification.MergeRight;
            }
            throw new InvalidOperationException("Bug in MergeAmount classification and validation");
        }
    }

    /// <summary>
    /// The <see cref="LaneMappingInputs"/> is a validated collection of <see cref="LaneMappingInput"/>, that is checked agains all rules in RULES-LaneMappingInputs.md
    /// </summary>
    public sealed class LaneMappingInputs {
        public ImmutableArray<LaneMappingInput> LaneMappings { get; }
        public LaneMappingInputs(IList<LaneMappingInput> laneMappings) {
            ValidateLaneMappings(laneMappings);
            LaneMappings = laneMappings.ToImmutableArray();
        }

        public static void ValidateLaneMappings(IList<LaneMappingInput> laneMappings) {
            ArgumentNullException.ThrowIfNull(laneMappings, nameof(laneMappings));
            if (laneMappings.Count == 0) throw new LaneValidationException("No lanes in mappings!");

            var leftBound = laneMappings[0];
            var rightBound = laneMappings[^1];
            if (rightBound.LeftAmount.IsMerge && !rightBound.RightAmount.IsMerge)
                throw new LaneValidationException("Right lane merges into the right edge");
            
            if(leftBound.RightAmount.IsMerge && !rightBound.LeftAmount.IsMerge) 
                throw new LaneValidationException("Left lane merges into the left edge");
            
            //Validate adjacency rules
            for (int i = 0; i < laneMappings.Count; i++) {
                var a = laneMappings[i-1];
                var b = laneMappings[i];

                if (a.LeftAmount.IsMerge & b.LeftAmount.IsMerge)
                    throw new LaneValidationException("Double merge into right");
                if (a.LeftAmount.IsMerge & b.RightAmount.IsMerge)
                    throw new LaneValidationException("Lanes merge into each other");
                if (a.RightAmount.IsMerge & b.RightAmount.IsMerge) 
                    throw new LaneValidationException("Double merge into left");
            }
            
        }
    }
}
