using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LanguageExt.TypeClasses;
using TranSimCS.Model;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Spline;

namespace TranSimCS.Worlds.Car {
    public struct LanePosition : IEquatable<LanePosition> {
        /// <summary>
        /// On which lane strip is the car currently driving? Null for off-road
        /// </summary>
        public LaneStrip? LaneStrip;
        /// <summary>
        /// Arc-length position of the car in the direction
        /// </summary>
        public float LaneArcLength = float.NaN;
        /// <summary>
        /// If true, the car goes the wrong way. Else it goes the right way. Determines which spline to use (forward vs reverse) for lateral and position.
        /// Tangential is reversed if true from the same spline, derived from the strip spline at interpolated T parameter
        /// </summary>
        public bool IsReverse = false;

        public LanePosition() { }
        public LanePosition(LaneStrip? laneStrip, float lanePosition = float.NaN, bool isReverse = false) {
            LaneStrip = laneStrip;
            LaneArcLength = lanePosition;
            IsReverse = isReverse;
        }

        public override bool Equals(object? obj) {
            return obj is LanePosition position && Equals(position);
        }

        public bool Equals(LanePosition other) {
            return EqualityComparer<LaneStrip>.Default.Equals(LaneStrip, other.LaneStrip) &&
                   LaneArcLength == other.LaneArcLength &&
                   IsReverse == other.IsReverse;
        }

        public override int GetHashCode() {
            return HashCode.Combine(LaneStrip, LaneArcLength, IsReverse);
        }

        public static bool operator ==(LanePosition left, LanePosition right) {
            return left.Equals(right);
        }

        public static bool operator !=(LanePosition left, LanePosition right) {
            return !(left == right);
        }
    }

    public class LanePositionConverter(TSWorld world) : JsonConverter<LanePosition> {
        public override LanePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var stripConverter = new StripRefConverter(world);

            var temp0 = reader;
            temp0.Read();
            if(temp0.TokenType == JsonTokenType.Null) return default;
            if(temp0.TokenType == JsonTokenType.StartArray) {
                //Read as a raw lane strip
                var strip = stripConverter.Read(ref reader, typeToConvert, options);
                return new LanePosition(strip);
            }

            LanePosition result = new LanePosition();
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "reverse":
                        reader0.Read();
                        result.IsReverse = reader0.GetBoolean();
                        break;
                    case "pos":
                        reader0.Read();
                        result.LaneArcLength = reader0.GetSingle();
                        break;
                    case "strip":
                        result.LaneStrip = stripConverter.Read(ref reader0, typeof(LaneStrip), options);
                        break;
                }
            });

            return result;
        }

        public override void Write(Utf8JsonWriter writer, LanePosition value, JsonSerializerOptions options) {
            writer.WriteStartObject();
            writer.WriteBoolean("reverse", value.IsReverse);
            if (float.IsFinite(value.LaneArcLength)) writer.WriteNumber("pos", value.LaneArcLength);
            if(value.LaneStrip != null) {
                var stripConverter = new StripRefConverter(world);
                writer.WritePropertyName("strip");
                stripConverter.Write(writer, value.LaneStrip, options);
            }
            writer.WriteEndObject();
        }
    }
}
