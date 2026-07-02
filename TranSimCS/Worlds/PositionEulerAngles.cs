using System;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Worlds {
    public struct PositionEulerAngles: IEquatable<PositionEulerAngles> {
        //Position
        public Vector3 Position; // World position of the node

        //Angle
        public int Azimuth; // Azimuth angle in the 2^32 field
        public float Inclination; // Inclination angle in radians
        public float Tilt; // Tilt angle in radians


        public static PositionEulerAngles Zero => new PositionEulerAngles(Vector3.Zero, 0);

        public PositionEulerAngles(Vector3 position, int azimuth, float inclination = 0f, float tilt = 0f) {
            Position = position;
            Azimuth = azimuth;
            Inclination = inclination;
            Tilt = tilt;
        }

        public override bool Equals(object? obj) {
            if (obj is PositionEulerAngles other) {
                return Position.Equals(other.Position) &&
                       Azimuth == other.Azimuth &&
                       Inclination.Equals(other.Inclination) &&
                       Tilt.Equals(other.Tilt);
            }
            return false;
        }
        public override int GetHashCode() {
            HashCode hash = new HashCode();
            hash.Add(Position);
            hash.Add(Azimuth);
            hash.Add(Inclination);
            hash.Add(Tilt);
            return hash.ToHashCode(); // Generate a hash code based on the properties of the node position
        }
        public static bool operator ==(PositionEulerAngles left, PositionEulerAngles right) {
            return left.Equals(right);
        }
        public static bool operator !=(PositionEulerAngles left, PositionEulerAngles right) {
            return !(left == right);
        }

        public Transform3 CalcReferenceFrame() => new(CalcReferenceMatrix());

        public static PositionEulerAngles FromPosTangentTilt(Vector3 pos, Vector3 tangent, float tilt) {
            var htangent = GeometryUtils.hypot2(tangent.X, tangent.Z);
            var inclination = MathF.Atan2(tangent.Y, htangent);
            var azimuthRadians = MathF.Atan2(tangent.X, tangent.Z);
            var azimuth = GeometryUtils.RadiansToField(azimuthRadians);
            return new PositionEulerAngles(pos, azimuth, inclination, tilt);
        }
        public static PositionEulerAngles FromPosTangentLateral(Vector3 pos, Vector3 tangent, Vector3 lateral) {
            var nrm = Vector3.Cross(lateral, tangent);
            nrm.Normalize();
            var ypr = Transform3.ToYawPitchRoll(lateral, nrm, tangent);

            return new PositionEulerAngles(pos, GeometryUtils.RadiansToField(ypr.X), ypr.Y, ypr.Z);
        }

        public bool Equals(PositionEulerAngles other) {
            return Position.Equals(other.Position)
                && Tilt .Equals(other.Tilt)
                && Inclination.Equals(other.Inclination)
                && Azimuth.Equals(other.Azimuth);
        }

        public Matrix CalcReferenceMatrix() => Matrix.CreateFromYawPitchRoll(GeometryUtils.FieldToRadians(Azimuth), -Inclination, Tilt) * Matrix.CreateTranslation(Position);

        public Vector3 GetTangential() {
            var az = GeometryUtils.FieldToRadians(Azimuth);
            var scP = MathF.SinCos(Inclination);
            var sP = scP.Sin;
            var cP = scP.Cos;
            var scA = MathF.SinCos(az);
            var sA = scA.Sin;
            var cA = scA.Cos;
            return new Vector3(sA * cP, sP, cA * cP);
        }

        public static (float Azimuth, float Inclination) Atan3(Vector3 v) {
            var a = MathF.Atan2(v.X, v.Z);
            var h = GeometryUtils.hypot2(v.X, v.Z);
            var p = MathF.Atan2(v.Y, h);
            return (a, p);
        }
    }
}
