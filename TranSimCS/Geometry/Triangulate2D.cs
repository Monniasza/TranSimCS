using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using MadWorldNL.EarCut.Logic;

namespace TranSimCS.Geometry {
    public static partial class Triangulate2D {
        public static List<int> TriangulatePolygon(IEnumerable<PointD> points) {
            return EarCut.Tessellate<double>(points.SelectMany(x => new double[] { x.x, x.y }).ToArray(), null, 2);
        }

        /// <summary>
        /// Triangulate the mesh with increasing y values
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static int[] LongitudinalTriangulate(PointD[] points) {
            var rows = new (PointD, int)[points.Length];
            for (int i = 0; i < rows.Length; i++) rows[i] = (points[i], i);

            Comparison<(PointD, int)> comparison = (a, b) => (a.Item1.y - b.Item1.y).CompareTo(0);
            var comparer = Comparer<(PointD, int)>.Create(comparison);

            //Sort the points by increasing Y
            var sortedRows = new (PointD, int)[points.Length];
            Array.Copy(rows, sortedRows, rows.Length);
            sortedRows.Sort(comparison);

            var count = points.Length;
            var index = 0;
            var ring = Node<(PointD, int)>.Circular(rows)[0];

            var valuecount = (points.Length - 2) * 3;
            var result = new int[valuecount];

            while(count > 2) {
                var currRow = sortedRows[index];
                var currNode = ring.Find(x => x.Item2 == currRow.Item2) ?? throw new Exception("Node not found. THIS METHOD IS BROKEN");

                //Right node found
                var prevNode = currNode.Prev ?? throw new Exception("No prev. THIS METHOD IS BROKEN");
                var prevIdx = prevNode.Value.Item2;
                var nextNode = currNode.Next ?? throw new Exception("No next. THIS METHOD IS BROKEN");
                var nextIdx = nextNode.Value.Item2;
                var currIdx = currRow.Item2;

                var prevVert = prevNode.Value.Item1;
                var nextVert = nextNode.Value.Item1;
                var currVert = currRow.Item1;

                if(Clockwise(prevVert, currVert, nextVert) < 0) {
                    //Reverse the order
                    (prevIdx, nextIdx) = (nextIdx, prevIdx);
                }

                result[index * 3] = currIdx;
                result[index * 3 + 1] = nextIdx;
                result[index * 3 + 2] = prevIdx;

                currNode.ClipOut();
                ring = nextNode;
                count--;
                index++;
            }
            
            return result;
        }

        public static double Clockwise(PointD a, PointD b, PointD c) {
            var abx = b.x - a.x;
            var aby = b.y - a.y;
            var acx = c.x - a.x;
            var acy = c.y - a.y;

            var cross = abx * acy - aby * acx;
            return cross;
        }

        public static int Circular(int value, int length) => ((value % length) + length) % length;
    }
}
