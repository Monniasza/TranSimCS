using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TranSimCS.Save2 {
    public sealed class IgnoreSavedTokenConverter<T>(Func<T> supplier) : JsonConverter<T> {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            reader.Skip();
            return supplier();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
            writer.WriteNullValue();
        }

        /// <summary>
        /// Creates an IgnoreSavedTokenConverter which returns a specific constant.
        /// </summary>
        /// <param name="value">value to be returned by the result</param>
        /// <returns>IgnoreSavedTokenConverter which returns a specific constant.</returns>
        public static IgnoreSavedTokenConverter<T> FromConstant(T value) => new(() => value);
        /// <summary>
        /// Creates an IgnoreSavedTokenConverter which returns the default value for a given type.
        /// The type should be nullable and/or structural.
        /// Assumption of non-nullability will be broken for non-null reference types.
        /// </summary>
        /// <returns>IgnoreSavedTokenConverter which returns a specific constant.</returns>
        public static IgnoreSavedTokenConverter<T> FromDefault() => FromConstant(default);
    }
    public static class IgnoreSaveDefaultCtorTokenConverter<T> where T: new() {
        public static readonly IgnoreSavedTokenConverter<T> Instance = new IgnoreSavedTokenConverter<T>(() => new T());
    }
}
