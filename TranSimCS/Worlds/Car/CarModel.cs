using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.ModelOld;

namespace TranSimCS.Worlds.Car {
    public static class CarModel {
        public static MultiMesh CreateModel() {
            var carMaterial = new SimpleMaterial() {
                EmissiveName = "car-emissive",
                TextureName = "car-albedo",
                BlendMode = MaterialBlendMode.Opaque,
            };

            Vector3 p1l = new(-1, 0,  2);
            Vector3 p2l = new(-1, 1,  2);
            Vector3 p3l = new(-1, 2,  0);
            Vector3 p4l = new(-1, 2, -1);
            Vector3 p5l = new(-1, 1, -2);
            Vector3 p6l = new(-1, 0, -2);
            Vector3 p1r = new( 1, 0,  2);
            Vector3 p2r = new( 1, 1,  2);
            Vector3 p3r = new( 1, 2,  0);
            Vector3 p4r = new( 1, 2, -1);
            Vector3 p5r = new( 1, 1, -2);
            Vector3 p6r = new( 1, 0, -2);

            VertexPositionColorTexture[] verts = [
                //0-11: Top
                new(p1l, Color.White, new(0, 0.5f)),
                new(p1r, Color.White, new(0, 0)),
                new(p2l, Color.White, new(0.125f, 0.5f)),
                new(p2r, Color.White, new(0.125f, 0)),
                new(p3l, Color.White, new(0.375f, 0.5f)),
                new(p3r, Color.White, new(0.375f, 0)),
                new(p4l, Color.White, new(0.5f, 0.5f)),
                new(p4r, Color.White, new(0.5f, 0)),
                new(p5l, Color.White, new(0.625f, 0.5f)),
                new(p5r, Color.White, new(0.625f, 0)),
                new(p6l, Color.White, new(0.75f, 0.5f)),
                new(p6r, Color.White, new(0.75f, 0)),

                //12 - 17: Left
                new(p1l, Color.White, new(0, 1)),
                new(p2l, Color.White, new(0, 0.75f)),
                new(p3l, Color.White, new(0.25f, 0.5f)),
                new(p4l, Color.White, new(0.375f, 0.5f)),
                new(p5l, Color.White, new(0.5f, 0.75f)),
                new(p6l, Color.White, new(0.5f, 1)),

                //18 - 23: Right
                new(p1r, Color.White, new(0, 1)),
                new(p2r, Color.White, new(0, 0.75f)),
                new(p3r, Color.White, new(0.25f, 0.5f)),
                new(p4r, Color.White, new(0.375f, 0.5f)),
                new(p5r, Color.White, new(0.5f, 0.75f)),
                new(p6r, Color.White, new(0.5f, 1)),
            ];
            ushort[] indices = [
                //Top strip
                0,1,2, 1,3,2,
                2,3,4, 3,5,4,
                4,5,6, 5,7,6,
                6,7,8, 7,9,8,
                8,9,10, 9,11,10,

                //Left half
                12,13,17, 13,14,17,
                14,15,17, 15,16,17,

                //Right half
                18,23,19, 19,23,20,
                20,23,21, 21,23,22
            ];

            MultiMesh multimesh = new MultiMesh();
            var renderBin = multimesh.GetOrCreateRenderBinForced(carMaterial);
            renderBin.DrawModel(verts, indices);
            return multimesh;
        }
    }
}
