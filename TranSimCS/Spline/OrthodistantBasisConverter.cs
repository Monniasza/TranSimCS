using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TranSimCS.Save2;

namespace TranSimCS.Spline {
    public sealed class OrthodistantBasisConverter : JsonConverter<OrthodistantBasis> {
        public override OrthodistantBasis Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {

            Bezier3 referenceSpline = default;
            Bezier3 normalSpline = default;
            Vector2 startEndPosition = Vector2.Zero;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "referencespline":
                        referenceSpline = ReadBezier3(ref reader0, options);
                        break;

                    case "normalspline":
                        normalSpline = ReadBezier3(ref reader0, options);
                        break;

                    case "startendposition":
                        startEndPosition = ReadVector2(ref reader0);
                        break;
                }
            });

            return new OrthodistantBasis(
                referenceSpline,
                normalSpline,
                startEndPosition);
        }

        public override void Write(
            Utf8JsonWriter writer,
            OrthodistantBasis value,
            JsonSerializerOptions options) {

            writer.WriteStartObject();

            writer.WritePropertyName("referenceSpline");
            WriteBezier3(writer, value.ReferenceSpline, options);

            writer.WritePropertyName("normalSpline");
            WriteBezier3(writer, value.NormalSpline, options);

            writer.WritePropertyName("startEndPosition");
            WriteVector2(writer, value.StartEndPosition);

            writer.WriteEndObject();
        }

        private static Bezier3 ReadBezier3(
            ref Utf8JsonReader reader,
            JsonSerializerOptions options) {

            Vector3 a = default;
            Vector3 b = default;
            Vector3 c = default;
            Vector3 d = default;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "a":
                        a = JsonSerializer.Deserialize<Vector3>(
                            ref reader0, options);
                        break;

                    case "b":
                        b = JsonSerializer.Deserialize<Vector3>(
                            ref reader0, options);
                        break;

                    case "c":
                        c = JsonSerializer.Deserialize<Vector3>(
                            ref reader0, options);
                        break;

                    case "d":
                        d = JsonSerializer.Deserialize<Vector3>(
                            ref reader0, options);
                        break;
                }
            });

            return new Bezier3(a, b, c, d);
        }

        private static void WriteBezier3(
            Utf8JsonWriter writer,
            Bezier3 value,
            JsonSerializerOptions options) {

            writer.WriteStartObject();

            writer.WritePropertyName("a");
            JsonSerializer.Serialize(writer, value.a, options);

            writer.WritePropertyName("b");
            JsonSerializer.Serialize(writer, value.b, options);

            writer.WritePropertyName("c");
            JsonSerializer.Serialize(writer, value.c, options);

            writer.WritePropertyName("d");
            JsonSerializer.Serialize(writer, value.d, options);

            writer.WriteEndObject();
        }

        private static Vector2 ReadVector2(ref Utf8JsonReader reader) {
            float x = 0;
            float y = 0;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                reader0.Read();

                switch (propertyName.ToLower()) {
                    case "x":
                        x = reader0.GetSingle();
                        break;

                    case "y":
                        y = reader0.GetSingle();
                        break;
                }
            });

            return new Vector2(x, y);
        }

        private static void WriteVector2(
            Utf8JsonWriter writer,
            Vector2 value) {

            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }
    }
}
