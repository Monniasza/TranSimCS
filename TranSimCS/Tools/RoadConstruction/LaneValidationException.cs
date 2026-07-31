using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Tools.RoadConstruction {
    public class LaneValidationException : ArgumentException {
        public LaneValidationException() {
        }

        public LaneValidationException(string? message) : base(message) {
        }

        public LaneValidationException(string? message, Exception? innerException) : base(message, innerException) {
        }

        public LaneValidationException(string? message, string? paramName) : base(message, paramName) {
        }

        public LaneValidationException(string? message, string? paramName, Exception? innerException) : base(message, paramName, innerException) {
        }

        protected LaneValidationException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
