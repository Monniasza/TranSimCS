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

        public OrthodistantLUT(OrthodistantBasis spline, int numPoints = 129, float minT = 0, float maxT = 1) {
            this.spline = spline;
            var nodes = new LUTKey[numPoints];
            var t = minT;
            var inc = (maxT - minT) / (numPoints - 1);
            for (int i = 0; i < numPoints; i++) {
                var node = new LUTKey(t, spline.Sample(t).O, 0);
                t += inc;
            }
            for (int i = 1; i < numPoints; i++) {
                var prevnode = nodes[i - 1];
                var nextnode = nodes[i];
                nextnode.Y.W = prevnode.Y.W + Vector3.Distance(prevnode.Y.ToXYZ(), nextnode.Y.ToXYZ());
                nodes[i] = nextnode;
            }
            this.Forward = new(nodes);

            var reverseNodes = new LUTKey[numPoints];
            for (int i = 0; i < numPoints; i++) {
                var node = nodes[^(i - 1)];
                node.Y.W = nodes[^1].Y.W - node.Y.W;
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
