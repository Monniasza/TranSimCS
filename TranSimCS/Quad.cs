using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public struct Quad {
        public VertexPositionColorTexture a;
        public VertexPositionColorTexture b;
        public VertexPositionColorTexture c;
        public VertexPositionColorTexture d;
        
        public Quad(VertexPositionColorTexture a, VertexPositionColorTexture b, VertexPositionColorTexture c, VertexPositionColorTexture d) {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
            this.a = new VertexPositionColorTexture(a, Color.White, new(0, 0));
            this.b = new VertexPositionColorTexture(b, Color.White, new(1, 0));
            this.c = new VertexPositionColorTexture(c, Color.White, new(1, 1));
            this.d = new VertexPositionColorTexture(d, Color.White, new(0, 1));
        }

        public static Quad operator +(Quad quad, Vector3 offset) {
            return new Quad(
                Geometry.OffsetVert(quad.a, offset),
                Geometry.OffsetVert(quad.b, offset),
                Geometry.OffsetVert(quad.c, offset),
                Geometry.OffsetVert(quad.d, offset)
            );
        }
        public static Quad operator -(Quad quad, Vector3 offset) {
            return new Quad(
                Geometry.SubVert(quad.a, offset),
                Geometry.SubVert(quad.b, offset),
                Geometry.SubVert(quad.c, offset),
                Geometry.SubVert(quad.d, offset)
            );
        }
    }
}