using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Clipper2Lib;
using MonoGame.Extended.Shapes;

namespace TranSimCS.Polygons {

    /// <summary>
    /// 
    /// </summary>
    public class Polygon {
        public readonly PathsD path;
        public readonly FillRule fillRule;

        public Polygon(PathsD path, FillRule fillRule) {
            this.path = path;
            this.fillRule = fillRule;
        }
        public Polygon(PathD path, FillRule fillRule) {
            this.path = [path];
            this.fillRule = fillRule;
        }
        public Polygon() {
            this.path = new PathsD();
            this.fillRule = FillRule.EvenOdd;
        }

        public Polygon Intersect(Polygon other) {
            var result = Clipper.Intersect(path, other.path, fillRule, 6);
            return new Polygon(result, fillRule);
        }
        public Polygon Union(Polygon other) {
            var result = Clipper.Union(path, other.path, fillRule, 6);
            return new Polygon(result, fillRule);
        }
        public Polygon Subtract(Polygon other) {
            var result = Clipper.Difference(path, other.path, fillRule, 6);
            return new Polygon(result, fillRule);
        }
        public Polygon Xor(Polygon other) {
            var result = Clipper.Xor(path, other.path, fillRule, 6);
            return new Polygon (result, fillRule);
        }

        public double Area() {
            return Clipper.Area(path);
        }
        public Polygon Simplify(double epsilon) {
            var result = Clipper.SimplifyPaths(path, epsilon);
            return new Polygon(result, fillRule);
        }

        public static Polygon Sum(FillRule rule, params Polygon[] polygons) {
            var path = new PathsD();
            var clipper = new ClipperD();
            foreach (var polygon in polygons) clipper.AddSubject(polygon.path);
            clipper.Execute(ClipType.Union, rule, path);
            return new Polygon(path, rule);
        }
        public static Polygon Intersection(FillRule rule, params Polygon[] polygons) {
            var path = new PathsD();
            var clipper = new ClipperD();
            foreach (var polygon in polygons) clipper.AddSubject(polygon.path);
            clipper.Execute(ClipType.Intersection, rule, path);
            return new Polygon(path, rule);
        }
        public Polygon SubtractMore(IEnumerable<Polygon> subtractends) {
            return new Polygon(Clipper.Difference(path, BalancedMerge(subtractends.Select(x => x.path), fillRule), fillRule), fillRule);
        }
        public static Polygon MultiSubtract(FillRule fillRule, IEnumerable<Polygon> addends, IEnumerable<Polygon> subtractends, int precision = 6) {
            var mergedAddends = BalancedMerge(addends.Select(x => x.path), fillRule);
            var mergedSubtractends = BalancedMerge(addends.Select(x => x.path), fillRule);
            return new Polygon(Clipper.Difference(mergedAddends, mergedSubtractends, fillRule, precision), fillRule);
        }
        public Polygon Offset(double expand, JoinType joinType = JoinType.Miter, EndType endType = EndType.Polygon, int miterLimit = 2, int precision = 2, double arcTolerance = 0) {
            var result = Clipper.InflatePaths(path, expand, joinType, endType, miterLimit, precision, arcTolerance);
            return new Polygon(result, fillRule);
        }

        public static PathsD BalancedMerge(IEnumerable<PathsD> paths, FillRule fillRule, int precision = 6) {
            //Initialize a queue
            Queue<PathsD> result = new Queue<PathsD>(paths);
            if (result.Count == 0) return new PathsD();
            while (result.Count > 1) {
                var take1 = result.Dequeue();
                var take2 = result.Dequeue();
                var union = Clipper.Union(take1, take2, fillRule, precision);
                result.Enqueue(union);
            }
            return result.Dequeue();
        }

        public static double Perimeter(PathD path) {
            double sum = 0;
            for (int i = 0; i < path.Count; i++) {
                var prev = path[i];
                var next = path[(i + 1) % path.Count];
                sum += prev.Distance(next);
            }
            return sum;
        }

        //Boolean operators
        public static Polygon operator &(Polygon a, Polygon b) => a.Intersect(b);
        public static Polygon operator |(Polygon a, Polygon b) => a.Union(b);
        public static Polygon operator ^(Polygon a, Polygon b) => a.Xor(b);
        public static Polygon operator -(Polygon a, Polygon b) => a.Subtract(b);

    }
}
