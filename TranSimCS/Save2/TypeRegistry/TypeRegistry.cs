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
    /// Expli
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class TypeRegistry<T> : JsonConverter<T> where T: ITypeRegistered<T>{
        public Dictionary<string, JsonConverter<T>> converters = [];

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

    [Serializable]
    public class ConverterNotFoundException : Exception {
        public ConverterNotFoundException() {
        }

        public ConverterNotFoundException(string? message) : base(message) {
        }

        public ConverterNotFoundException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }

    public interface ITypeRegistered<T> where T: ITypeRegistered<T> {
        (string TypeId, TypeRegistry<T> TypeRegistry) TypeInfo();
    }
}
