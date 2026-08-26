using System.Numerics;
using SixLabors.ImageSharp.PixelFormats;
using TranSimCS.Geometry;
using TranSimCS.SilkNet;

namespace TranSimCS.Model {
    public struct QuadOld {
        public Vertex a;
        public Vertex b;
        public Vertex c;
        public Vertex d;
        
        public QuadOld(Vertex a, Vertex b, Vertex c, Vertex d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public QuadOld(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            this.a = new Vertex(a, Colors.White, new(0, 0));
            this.b = new Vertex(b, Colors.White, new(1, 0));
            this.c = new Vertex(c, Colors.White, new(1, 1));
            this.d = new Vertex(d, Colors.White, new(0, 1));
        }

        public QuadOld(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rgba32 color) : this(a, b, c, d) {
            this.a = new Vertex(a, color, new(0, 0));
            this.b = new Vertex(b, color, new(1, 0));
            this.c = new Vertex(c, color, new(1, 1));
            this.d = new Vertex(d, color, new(0, 1));
        }

        public static QuadOld operator +(QuadOld quad, Vector3 offset) {
            return new QuadOld(
                GeometryUtils.OffsetVert(quad.a, offset),
                GeometryUtils.OffsetVert(quad.b, offset),
                GeometryUtils.OffsetVert(quad.c, offset),
                GeometryUtils.OffsetVert(quad.d, offset)
            );
        }
        public static QuadOld operator -(QuadOld quad, Vector3 offset) {
            return new QuadOld(
                GeometryUtils.SubVert(quad.a, offset),
                GeometryUtils.SubVert(quad.b, offset),
                GeometryUtils.SubVert(quad.c, offset),
                GeometryUtils.SubVert(quad.d, offset)
            );
        }
    }
}