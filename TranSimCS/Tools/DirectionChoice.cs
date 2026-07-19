namespace TranSimCS.Tools {
    /// <summary>
    /// Determines how lane directions are obtaines by <see cref="SegmentTool"/>
    /// </summary>
    public enum DirectionChoice {
        /// <summary>
        /// Lane directions will be determined by <see cref="LaneMappings.IsReverseLaneHeuristic(Roads.Node.Lane)"/>
        /// </summary>
        Auto = 0,
        /// <summary>
        /// All lanes will be forward in the build direction
        /// </summary>
        Forward = 1,
        /// <summary>
        /// All lanes will be reverse in the build direction
        /// </summary>
        Reverse = 2,
        /// <summary>
        /// Number of valid <see cref="DirectionChoice"/> values. It is not a valid value.
        /// </summary>
        Count = 3
    }
    /// <summary>
    /// Algorithms for <see cref="DirectionChoice"/>
    /// </summary>
    public static class DirectionChoiceMethods {
        /// <summary>
        /// Cycles through direction choices
        /// </summary>
        /// <param name="directionChoice">previous direction choice</param>
        /// <returns>next direction choice option</returns>
        public static DirectionChoice Next(this DirectionChoice directionChoice) {
            directionChoice++;
            if (directionChoice == DirectionChoice.Count) return DirectionChoice.Auto;
            return directionChoice;
        }
    }
}
