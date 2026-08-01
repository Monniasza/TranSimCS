using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;

namespace TranSimCS.Spline {
    public struct TangentArc {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public Vector3 Tangent;

        public Vector3[] Generate(int count = 65) {
            Vector3[] result = new Vector3[count];
            Array.Fill(result, StartPosition);

            var step = 1f / (count - 1);


            var chord = EndPosition - StartPosition;
            var chordLength = chord.Length();
            if(chordLength < 0.001) 
                //It's degenerate
                return result;
            

            var rawNormal = Vector3.Cross(Tangent, chord);
            if(rawNormal.LengthSquared() < 0.000001)
                //Tangent points towards, away or nowhere to endpoint
                return result;

            var inward = Vector3.Cross(rawNormal, Tangent);
            if (inward.LengthSquared() < 0.000001)
                //Radial calculation failed
                return result;
            inward.Normalize();

            var radius = chord.LengthSquared() / (2 * Vector3.Dot(chord, inward));
            var normal = rawNormal.Normalized();
            var center = StartPosition + radius * inward;
            var ra = StartPosition - center;
            var rb = EndPosition - center;
            var angle = MathF.Atan2(Vector3.Dot(normal, Vector3.Cross(ra, rb)), Vector3.Dot(ra, rb));
            
        }
    }
}
