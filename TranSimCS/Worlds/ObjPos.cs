using System;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Worlds {
    public struct ObjPos: IEquatable<ObjPos> {
        //Position
        public Vector3 Position { get; set; } // World position of the node

        //Angle
        public int Azimuth { get; set; } // Azimuth angle in the 2^32 field
        public float Inclination { get; set; } // Inclination angle in radians
        public float Tilt { get; set; } // Tilt angle in radians


        public static ObjPos Zero => new ObjPos(Vector3.Zero, 0);

        public ObjPos(Vector3 position, int azimuth, float inclination = 0f, float tilt = 0f) {
            Position = position;
            Azimuth = azimuth;
            Inclination = inclination;
            Tilt = tilt;
        }

        public override bool Equals(object? obj) {
            if (obj is ObjPos other) {
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
        public static bool operator ==(ObjPos left, ObjPos right) {
            return left.Equals(right);
        }
        public static bool operator !=(ObjPos left, ObjPos right) {
            return !(left == right);
        }

        public Transform3 CalcReferenceFrame() {
            Matrix matrix = Matrix.CreateFromYawPitchRoll(GeometryUtils.FieldToRadians(Azimuth), -Inclination, Tilt) * Matrix.CreateTranslation(Position);
            return new Transform3(matrix);
        }

        public static ObjPos FromPosTangentTilt(Vector3 pos, Vector3 tangent, float tilt) {
            var htangent = GeometryUtils.hypot2(tangent.X, tangent.Z);
            var inclination = MathF.Atan2(tangent.Y, htangent);
            var azimuthRadians = MathF.Atan2(tangent.X, tangent.Z);
            var azimuth = GeometryUtils.RadiansToField(azimuthRadians);
            return new ObjPos(pos, azimuth, inclination, tilt);
        }
        public static ObjPos FromPosTangentLateral(Vector3 pos, Vector3 tangent, Vector3 lateral) {
            var nrm = Vector3.Cross(lateral, tangent);
            nrm.Normalize();
            var ypr = Transform3.ToYawPitchRoll(lateral, nrm, tangent);

            return new ObjPos(pos, GeometryUtils.RadiansToField(ypr.X), ypr.Y, ypr.Z);
        }

        public bool Equals(ObjPos other) {
            return Position.Equals(other.Position)
                && Tilt .Equals(other.Tilt)
                && Inclination.Equals(other.Inclination)
                && Azimuth.Equals(other.Azimuth);
        }
    }
}
