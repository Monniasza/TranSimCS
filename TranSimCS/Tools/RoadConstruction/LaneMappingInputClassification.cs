using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Tools.RoadConstruction {
    /// <summary>
    /// Defines a classification of a <see cref="LaneMappingInput"/>.
    /// </summary>
    public enum LaneMappingInputClassification {
        Straight, MergeLeft, MergeRight, ExitLeft, ExitRight, ExitLeftRight, End
    }

    /// <summary>
    /// Provide methods to classify <see cref="LaneMappingInputClassification"/>s
    /// </summary>
    public static class LaneMappingInputClassificationMethods {
        /// <summary>
        /// Determines if a <see cref="LaneMappingInputClassification"/> is a merge
        /// </summary>
        public static bool IsMerge(this LaneMappingInputClassification inputClassification) => inputClassification switch {
            LaneMappingInputClassification.MergeLeft => true,
            LaneMappingInputClassification.MergeRight => true,
            _ => false
        };
        /// <summary>
        /// Determines if a <see cref="LaneMappingInputClassification"/> is an exit
        /// </summary>
        public static bool IsExit(this LaneMappingInputClassification inputClassification) => inputClassification switch {
            LaneMappingInputClassification.ExitLeft => true,
            LaneMappingInputClassification.ExitRight => true,
            LaneMappingInputClassification.ExitLeftRight => true,
            _ => false
        };
    }
}
