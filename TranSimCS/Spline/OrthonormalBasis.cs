using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
    public struct OrthonormalBasis : IEquatable<OrthonormalBasis> {
        public Bezier3 CenterSpline;
        public Bezier3 NormalSpline;

        public OrthonormalBasis(Bezier3 centerSpline, Bezier3 normalSpline) {
            CenterSpline = centerSpline;
            NormalSpline = normalSpline;
        }

        public override bool Equals(object? obj) {
            return obj is OrthonormalBasis basis && Equals(basis);
        }

        public bool Equals(OrthonormalBasis other) {
            return EqualityComparer<Bezier3>.Default.Equals(CenterSpline, other.CenterSpline) &&
                   EqualityComparer<Bezier3>.Default.Equals(NormalSpline, other.NormalSpline);
        }

        public override int GetHashCode() {
            return HashCode.Combine(CenterSpline, NormalSpline);
        }

        public Transform3 Sample(float t) {
            var sampledPosition = CenterSpline[t];
            var sampledNormal = NormalSpline[t];
            var sampledTangent = CenterSpline.Tangential(t);
            var binormal = Vector3.Cross(sampledNormal, sampledTangent).Normalized();
            var normal = Vector3.Cross(sampledTangent, binormal).Normalized();
            var tangent = sampledTangent.Normalized();
            return new Transform3(binormal, normal, tangent, sampledPosition);
        }
        public Transform3[] MultiSample(int accuracy) {
            var result = new Transform3[accuracy];
            float step = 1 / (accuracy - 1.0f);
            for (int i = 0; i < accuracy; i++) result[i] = Sample(i * step);
            return result;
        }

        public static bool operator ==(OrthonormalBasis left, OrthonormalBasis right) {
            return left.Equals(right);
        }

        public static bool operator !=(OrthonormalBasis left, OrthonormalBasis right) {
            return !(left == right);
        }
    }
}
