using System;
using System.Reflection;

namespace TranSimCS.Model.OBJ {
    public enum DuplicatePolicy {
        Fail, Discard, Replace
    }

    public static class DuplicatePolicyMethods {
        public static T Replace<T>(this DuplicatePolicy policy, T existingValue, T newValue) {
            return policy switch {
                DuplicatePolicy.Fail => throw new AmbiguousMatchException(),
                DuplicatePolicy.Discard => existingValue,
                DuplicatePolicy.Replace => newValue,
                _ => throw new ArgumentException("Invalid DuplicatePolicy: " + policy),
            };
        }
    }
}
