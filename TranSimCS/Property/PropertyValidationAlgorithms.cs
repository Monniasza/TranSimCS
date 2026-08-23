using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Property {
    public static class PropertyValidationAlgorithms {
        public static void RequireFinitePositive(IProperty<float> prop, float oldValue, float newValue) {
            if (!float.IsFinite(newValue) || newValue <= 0) throw new ArgumentException($"Value is not a positive real number: {newValue}");
        }
        public static void RequireFinitePositiveOrNull(IProperty<float?> prop, float? oldValue, float? newValue) {
            if (newValue == null) return;
            if (!float.IsFinite(newValue.Value) || newValue <= 0) throw new ArgumentException($"Value is not a positive real number: {newValue}");
        }
        public static void RequireFiniteOrNull(IProperty<float?> prop, float? oldValue, float? newValue) {
            if (newValue == null) return;
            if (!float.IsFinite(newValue.Value)) throw new ArgumentException($"Value is not a positive real number: {newValue}");
        }
    }
}
