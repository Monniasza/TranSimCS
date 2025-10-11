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
            var result = Clipper.Intersect(path, other.path, fillRule);
            return new Polygon(result, fillRule);
        }
        public Polygon Union(Polygon other) {
            var result = Clipper.Union(path, other.path, fillRule);
            return new Polygon(result, fillRule);
        }
        public Polygon Subtract(Polygon other) {
            var result = Clipper.Difference(path, other.path, fillRule);
            return new Polygon(result, fillRule);
        }
        public Polygon Xor(Polygon other) {
            var result = Clipper.Xor(path, other.path, fillRule);
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
        public static Polygon MultiSubtract(FillRule rule, IEnumerable<Polygon> addends, IEnumerable<Polygon> subtractends) {
            var path = new PathsD();
            var clipper = new ClipperD();
            foreach (var polygon in addends) clipper.AddSubject(polygon.path);
            foreach (var polygon in subtractends) clipper.AddSubject(polygon.path);
            clipper.Execute(ClipType.Intersection, rule, path);
            return new Polygon(path, rule);
        }
        public Polygon Offset(double expand, JoinType joinType = JoinType.Miter, EndType endType = EndType.Polygon, int miterLimit = 2, int precision = 2, double arcTolerance = 0) {
            var result = Clipper.InflatePaths(path, expand, joinType, endType, miterLimit, precision, arcTolerance);
            return new Polygon(result, fillRule);
        }

        //Boolean operators
        public static Polygon operator &(Polygon a, Polygon b) => a.Intersect(b);
        public static Polygon operator |(Polygon a, Polygon b) => a.Union(b);
        public static Polygon operator ^(Polygon a, Polygon b) => a.Xor(b);
        public static Polygon operator -(Polygon a, Polygon b) => a.Subtract(b);

    }
}
