using System;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// Thrown when a <see cref="LaneMapping"/> is internally inconsistent and so cannot be committed.
    /// <para>
    /// The message is written to be shown to the user directly, so it names the offending lane and says
    /// what is wrong with it. Callers that need to report the problem rather than abort should use
    /// <see cref="LaneMapping.TryValidate"/>.
    /// </para>
    /// </summary>
    public class LaneMappingException : InvalidOperationException {
        public LaneMappingException() {
        }

        public LaneMappingException(string? message) : base(message) {
        }

        public LaneMappingException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}
