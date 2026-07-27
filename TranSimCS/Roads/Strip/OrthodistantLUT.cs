using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public class OrthodistantLUT {
        public readonly OrthodistantBasis spline;
        public LUT Forward {  get; private set; }
        public LUT Reverse { get; private set; }
        public float Length { get; private set; }

        public OrthodistantLUT(OrthodistantBasis spline, int numPoints = 129, float minT = 0, float maxT = 1) {
            this.spline = spline;

            //Sample the spline
            var samples = new Vector4[numPoints];
            var t = minT;
            var inc = (maxT - minT) / (numPoints - 1);
            for (int i = 0; i < numPoints; i++) {
                var sample = spline.SamplePosition(t);
                samples[i] = new(sample, t);
                t += inc;
            }

            float cumulativeDistance = 0;
            Vector4 previousSample = samples[0];
            LUTKey[] keys = new LUTKey[numPoints];
            for (int i = 0; i < numPoints; i++) {
                var sample = samples[i];
                var distance = Vector3.Distance(sample.ToXYZ(), previousSample.ToXYZ());
                cumulativeDistance += distance;
                keys[i] = new(cumulativeDistance, sample);
                previousSample = sample;
            }
            this.Forward = new(keys);
            this.Length = cumulativeDistance;

            var reverseNodes = new LUTKey[numPoints];
            for (int i = 0; i < numPoints; i++) {
                var node = keys[^(i + 1)];
                node.X = cumulativeDistance - node.X;
                reverseNodes[i] = node;
            }
            this.Reverse = new(reverseNodes);
        }
        /*public float FindClosest(Vector3 vector) {
            int closestIndex = 0;
            for(int i = 1; i < this.nodes.Count; i++) {
                var ln
                var prevNode = this.nodes[closestIndex];
                var node = this.nodes[i];
                var prevDist = Vector3.Distance(prevNode.pos, vector);
                var dist = Vector3.Distance(node.pos, vector);
                if (dist < prevDist) closestIndex = i;
            }

            //Find the other index
            if(closestIndex ==)
        }*/
    }
}
