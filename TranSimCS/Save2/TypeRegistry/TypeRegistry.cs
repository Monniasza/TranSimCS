using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TranSimCS.Save2.TypeRegistry {
    /// <summary>
    /// An explicit polymorphic JSON converter that serializes and deserializes objects
    /// using string-based runtime type identifiers.
    /// </summary>
    /// <remarks>
    /// Objects are serialized in the following format:
    /// <code>
    /// {
    ///     "type": "example_type",
    ///     "data": { ... }
    /// }
    /// </code>
    ///
    /// The <c>type</c> field determines which converter from
    /// <see cref="converters"/> will be used to process the
    /// <c>data</c> field.
    ///
    /// The <c>type</c> property must appear before <c>data</c>
    /// during deserialization.
    /// </remarks>
    /// <typeparam name="T">
    /// Base type handled by this registry.
    /// Must implement <see cref="ITypeRegistered{T}"/>.
    /// </typeparam>
    public sealed class TypeRegistry<T> : JsonConverter<T> where T: ITypeRegistered<T>{
        /// <summary>
        /// Maps type identifiers to converters responsible for
        /// serializing and deserializing that type.
        /// </summary>
        public Dictionary<string, JsonConverter<T>> converters = [];

        /// <summary>
        /// Deserializes a polymorphic object from JSON.
        /// </summary>
        /// <param name="reader">JSON reader.</param>
        /// <param name="typeToConvert">Requested target type.</param>
        /// <param name="options">Serializer options.</param>
        /// <returns>The deserialized object.</returns>
        /// <exception cref="JsonException">
        /// Thrown when the JSON structure is invalid.
        /// Thrown when no converter exists for the provided type identifier.
        /// </exception>
        /// <remarks>
        /// Expected JSON format:
        /// <code>
        /// {
        ///     "type": "type_id",
        ///     "data": { ... }
        /// }
        /// </code>
        ///
        /// The <c>type</c> property must be encountered before
        /// the <c>data</c> property.
        /// </remarks>
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            string? type = null;
            bool valueExists = false;
            T value = default;
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, name) => {
                switch (name) {
                    case "type":
                        //Read an object type
                        if (type != null) JsonProcessor.Fail(reader0, "Type already provided");
                        JsonProcessor.AssertTokenType(ref reader0, JsonTokenType.String);
                        type = reader0.GetString();
                        break;
                    case "data":
                        //Read actual data
                        if (type == null) JsonProcessor.Fail(reader0, "Type is not provided or is not the first value");
                        if (valueExists) JsonProcessor.Fail(reader0, "Value already exists");
                        if (converters.TryGetValue(type, out var converter)) {
                            //Found a converter
                            value = converter.Read(ref reader0, converter.Type, options);
                            valueExists = true;
                        }else 
                            //Converter not found
                            JsonProcessor.Fail(reader0, $"Converter {type} not found");
                        break;
                    default:
                        JsonProcessor.Fail(reader0, $"Unexpected key {name}");
                        break;
                }
            });
            if (!valueExists) JsonProcessor.Fail(reader, "Data not found");
            return value;
        }

        /// <summary>
        /// Serializes a polymorphic object to JSON.
        /// </summary>
        /// <param name="writer">JSON writer.</param>
        /// <param name="value">Value to serialize.</param>
        /// <param name="options">Serializer options.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the object does not belong to this registry.
        /// </exception>
        /// <exception cref="ConverterNotFoundException">
        /// Thrown when no converter exists for the object's type identifier.
        /// </exception>
        /// <remarks>
        /// The serialized format is:
        /// <code>
        /// {
        ///     "type": "type_id",
        ///     "data": { ... }
        /// }
        /// </code>
        /// </remarks>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
            var typeInfo = value.TypeInfo();
            if (typeInfo.TypeRegistry != this) throw new ArgumentException("The value does not belong to this TypeRegistry");
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            writer.WriteStringValue(typeInfo.TypeId);
            writer.WritePropertyName("data");
            if (converters.TryGetValue(typeInfo.TypeId, out var converter))
                converter.Write(writer, value, options);
            else
                throw new ConverterNotFoundException($"Converter {typeInfo.TypeId} not found");
                writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Exception thrown when a converter for a requested type identifier
    /// does not exist in a <see cref="TypeRegistry{T}"/>.
    /// </summary>
    [Serializable]
    public class ConverterNotFoundException : Exception {
        /// <summary>
        /// Creates an empty exception.
        /// </summary>
        public ConverterNotFoundException() {}

        /// <summary>
        /// Creates an exception with a custom message.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public ConverterNotFoundException(string? message) : base(message) {}

        /// <summary>
        /// Creates an exception with a custom message and inner exception.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public ConverterNotFoundException(string? message, Exception? innerException) : base(message, innerException) {}
    }

    /// <summary>
    /// Implemented by types that participate in a
    /// <see cref="TypeRegistry{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Base type associated with the registry.
    /// </typeparam>
    public interface ITypeRegistered<T> where T : ITypeRegistered<T> {

        /// <summary>
        /// Returns runtime type information used for polymorphic serialization.
        /// </summary>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <c>TypeId</c> — String identifier written to JSON.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <c>TypeRegistry</c> — Registry responsible for handling the type.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        (string TypeId, TypeRegistry<T> TypeRegistry) TypeInfo();  
    }
}
