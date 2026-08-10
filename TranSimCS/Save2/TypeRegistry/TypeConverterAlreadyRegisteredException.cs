using System;
using System.Runtime.Serialization;

namespace TranSimCS.Save2.TypeRegistry {
    [Serializable]
    internal class TypeConverterAlreadyRegisteredException : Exception {
        public TypeConverterAlreadyRegisteredException() {
        }

        public TypeConverterAlreadyRegisteredException(string? message) : base(message) {
        }

        public TypeConverterAlreadyRegisteredException(string? message, Exception? innerException) : base(message, innerException) {
        }

        protected TypeConverterAlreadyRegisteredException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

    }
}