using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS.Render {
    public class DLNode<T> {
        public static DLNode<T> CreateCircular(IEnumerable<T> list) {
            var nodes = list.Select(x => new DLNode<T>(x)).ToArray();
            for (int i = 0; i < nodes.Length; i++) {
                var currNode = nodes[i];
                var nextNode = nodes[(i + 1) % nodes.Length];
                currNode.Next = nextNode;
            }
            return nodes[0];
        }


        private DLNode<T> _prev;
        private DLNode<T> _next;
        public T val;

        public DLNode(T value) {
            val = value;
        }

        public DLNode<T> Prev {
            get => _prev;
            set {
                var oldPrev = _prev;
                var newPrev = value;
                oldPrev?._next = null;
                newPrev?._next = this;
                _prev = value; 
            }
        }
        public DLNode<T> Next {
            get => _next; set {
                var oldNext = _next;
                var newNext = value;
                oldNext?._prev = this;
                newNext?._prev = this;
                _next = value;
            }
        }

        public void ClipOut() {
            Prev.Next = Next;
            Next.Prev = Prev;
            Prev = null;
            Next = null;
        }
    }
    public static class EarClipping {
        public static void DrawEarClipping(IRenderBin mesh, params int[] indices) {

        }

        public static void DrawEarClipping(IRenderBin mesh, params VertexPositionColorTexture[] verts) {
            var normal = Geometry.NormalPoly(verts.Select(v => v.Position).ToArray());

            var addedVerts = verts.Select(v => (mesh.AddVertex(v), v)).ToArray();
            var workingList = DLNode<(int, VertexPositionColorTexture)>.CreateCircular(addedVerts);
            var length = verts.Length;
            var currentNode = workingList;
            while(length > 3) {
                //Check if the current vertex is an ear. It is an ear if none of the other vertices are on the triangle
                var prevVert = currentNode.Prev.val;
                var nextVert = currentNode.Next.val;
                var currVert = currentNode.val;

                var prevPos = prevVert.Item2.Position;
                var nextPos = nextVert.Item2.Position;
                var currPos = currVert.Item2.Position;

                var checkNode = currentNode.Next.Next;
                bool isEar = true;

                //If the vertex is concave, it's not an ear. That means that prev->curr is counterclockwise of curr->next
                var PtoC = currPos - prevPos;
                var CtoN = prevPos - nextPos;
                int negIfConcave = Geometry.CompareRotary(PtoC, CtoN, normal);
                if(negIfConcave < 0) isEar = false;

                while(isEar && checkNode != currentNode.Prev) {
                    //Iterate over vertices to check them
                    var checkPos = checkNode.val.Item2.Position;
                    var check = IsOnTriangle(prevPos, currPos, nextPos, checkPos);
                    if (check) isEar = false;
                }

                if (isEar) {
                    //The vertex is an ear, clip it
                    var prevIndex = prevVert.Item1;
                    var nextIndex = nextVert.Item1;
                    var currIndex = currVert.Item1;
                    mesh.DrawTriangle(prevIndex, nextIndex, currIndex);
                    currentNode.ClipOut();
                    length--;
                }

                currentNode = currentNode.Next;
            }
        }

        public static bool IsOnTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 v) {
            var normal = Vector3.Cross(a - b, a - c);
            if (normal == Vector3.Zero) return false;
            normal.Normalize();
            float discard = 0;
            var ray = new Ray(v, normal);
            return Geometry.RayIntersectsTriangle(ray, a, b, c, out discard, float.NegativeInfinity);
        }
    }
}
