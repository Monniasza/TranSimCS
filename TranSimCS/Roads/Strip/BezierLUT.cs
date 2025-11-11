using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Spline;

namespace TranSimCS.Roads.Strip {
    public struct BezierLUTNode(float t, float dist, Vector3 pos) {
        public float t;
        public float dist;
        public Vector3 pos;
    }
    public class BezierLUT {
        public readonly Bezier3 spline;
        public readonly ReadOnlyCollection<BezierLUTNode> nodes;
        public BezierLUT(Bezier3 spline, int numPoints = 129, float minT = 0, float maxT = 1) {
            this.spline = spline;
            var nodes = new BezierLUTNode[numPoints];
            var t = minT;
            var inc = (maxT - minT) / (numPoints - 1);
            for (int i = 0; i < numPoints; i++) {
                var node = new BezierLUTNode(t, 0, spline[t]);
                t += inc;
            }
            for (int i = 1; i < numPoints; i++) {
                var prevnode = nodes[i - 1];
                var nextnode = nodes[i];
                nextnode.dist = prevnode.dist + Vector3.Distance(prevnode.pos, nextnode.pos);
                nodes[i] = nextnode;
            }
            this.nodes = new(nodes);
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
