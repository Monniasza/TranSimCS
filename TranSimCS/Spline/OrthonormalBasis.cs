using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
    public struct OrthonormalBasis {
        public Bezier3 CenterSpline;
        public Bezier3 NormalSpline;

        public Transform3 Sample(float t) {
            var sampledPosition = CenterSpline[t];
            var sampledNormal = NormalSpline[t];
            var sampledTangent = CenterSpline.Tangential(t);
            var binormal = Vector3.Cross(sampledTangent, sampledNormal).Normalized();
            var normal = Vector3.Cross(sampledTangent, binormal).Normalized();
            var tangent = sampledTangent.Normalized();
            return new Transform3(binormal, normal, tangent, sampledPosition);
        }
    }
}
