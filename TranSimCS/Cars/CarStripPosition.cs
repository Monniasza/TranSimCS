using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.TypeClasses;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2;
using TranSimCS.Spline;

namespace TranSimCS.Cars {
    public class CarStripPosition : CarPosition, IEquatable<CarStripPosition>{
        /// <summary>
        /// On which lane strip is the car currently driving? Null for off-road
        /// </summary>
        public LaneStrip LaneStrip { get; private set; }
        /// <summary>
        /// Arc-length position of the car in the direction
        /// </summary>
        public float LaneArcLength { get; private set; }
        /// <summary>
        /// If true, the car goes the wrong way. Else it goes the right way. Determines which spline to use (forward vs reverse) for lateral and position.
        /// Tangential is reversed if true from the same spline, derived from the strip spline at interpolated T parameter
        /// </summary>
        public bool IsReverse { get; private set; }

        
        public CarStripPosition(LaneStrip laneStrip, float lanePosition = 0, bool isReverse = false) {
            ArgumentNullException.ThrowIfNull(laneStrip, nameof(laneStrip));
            if(!float.IsFinite(lanePosition)) throw new ArgumentException("Invalid lanePosition");
            LaneStrip = laneStrip;
            LaneArcLength = lanePosition;
            IsReverse = isReverse;
        }

        public override bool Equals(object? obj) {
            return obj is CarStripPosition position && Equals(position);
        }

        public bool Equals(CarStripPosition? other) {
            return other != null &&
                   LaneStrip == other.LaneStrip &&
                   LaneArcLength == other.LaneArcLength &&
                   IsReverse == other.IsReverse;
        }

        public override int GetHashCode() {
            return HashCode.Combine(LaneStrip, LaneArcLength, IsReverse);
        }

        public override CarStripPosition? Advance(float amount) {
            //Validate the current state
            if (LaneStrip.Road == null) return null;

            return new(LaneStrip, LaneArcLength + amount, IsReverse);
        }

        public override IEnumerable<CarPosition> FindNext(SegmentHalf half) => RouteMethods.FindNext(LaneStrip, IsReverse, half);

        private bool IsReverseToRoad => LaneStrip.IsReverse() ^ IsReverse;

        public override float CurrentPosition() => LaneArcLength;

        public override float MaxPosition() => LaneStrip.SplineLUT.Length;

        public override LUT GetPositionLookup() => IsReverseToRoad ? LaneStrip.SplineLUT.Reverse : LaneStrip.SplineLUT.Forward;

        public override Transform3 GetPositionFrame(float t) {
            var result = LaneStrip.SplineLUT.spline.SampleFrame(t);
            if (IsReverseToRoad) result = result.Around();
            return result;
        }

        public const string StripTypeName = "CarStripPosition";
        public override string TypeName() => StripTypeName;

        public override Guid SegmentName() => LaneStrip.Guid;

        public static bool operator ==(CarStripPosition? left, CarStripPosition? right) {
            return ReferenceEquals(null, left) ? ReferenceEquals(null, right) : left.Equals(right);
        }

        public static bool operator !=(CarStripPosition? left, CarStripPosition? right) {
            return !(left == right);
        }
    }

    public class LanePositionConverter() : JsonConverter<CarStripPosition?> {
        public override CarStripPosition? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var stripConverter = (StripRefConverter)options.GetConverter(typeof(LaneStrip));

            var temp0 = reader;
            temp0.Read();
            if(temp0.TokenType == JsonTokenType.Null) return default;
            if(temp0.TokenType == JsonTokenType.StartArray) {
                //Read as a raw lane strip
                var strip = stripConverter.Read(ref reader, typeToConvert, options);
                if(strip == null) return null;
                return new CarStripPosition(strip);
            }

            bool isReverse = false;
            float laneArcLength = 0;
            LaneStrip? laneStrip = null;

            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, propertyName) => {
                switch (propertyName.ToLower()) {
                    case "reverse":
                        reader0.Read();
                        isReverse = reader0.GetBoolean();
                        break;
                    case "pos":
                        reader0.Read();
                        laneArcLength = reader0.GetSingle();
                        break;
                    case "strip":
                        laneStrip = stripConverter.Read(ref reader0, typeof(LaneStrip), options);
                        break;
                }
            });

            if (laneStrip == null) return null;
            return new CarStripPosition(laneStrip, laneArcLength, isReverse);
        }

        public override void Write(Utf8JsonWriter writer, CarStripPosition? value, JsonSerializerOptions options) {
            if(value == null) {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteBoolean("reverse", value.IsReverse);
            if (float.IsFinite(value.LaneArcLength)) writer.WriteNumber("pos", value.LaneArcLength);
            if(value.LaneStrip != null) {
                var stripConverter = (StripRefConverter)options.GetConverter(typeof(LaneStrip));
                writer.WritePropertyName("strip");
                stripConverter.Write(writer, value.LaneStrip, options);
            }
            writer.WriteEndObject();
        }
    }
}
