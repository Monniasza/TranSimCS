using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.SilkNet;

namespace TranSimCS.TrafficLights {
    internal class TrafficLightMeshes {
        public static readonly Mesh Red = Create(Colors.Red);
        public static readonly Mesh Green = Create(Colors.Green);
        public static readonly SimpleMaterial Texture = SimpleMaterial.NewEmissive("tlight.png");

        private static Mesh Create(Color c) {
            //     2-----3
            //    /|    /|
            //   6-+---7 |
            //   | 0---+-1
            //   |/    |/
            //   4-----5

            float s = 0.5f;
            Vertex[] verts = [
                //Front (towards the vehicle)
                new(new(-s, -s,  s), c, new(0, 0)),
                new(new( s, -s,  s), c, new(0, 1)),
                new(new(-s,  s,  s), c, new(1, 0)),
                new(new( s,  s,  s), c, new(1, 1)),
                
                //Rear (towards the intersection)
                new(new(-s, -s, -s), c, new(0, 0)),
                new(new( s, -s, -s), c, new(0, 1)),
                new(new(-s,  s, -s), c, new(1, 0)),
                new(new( s,  s, -s), c, new(1, 1)),

                //Dedicated black rear
                new(new(-s, -s, -s), c, new(0, 0)),
                new(new( s, -s, -s), c, new(0, 0)),
                new(new(-s,  s, -s), c, new(0, 0)),
                new(new( s,  s, -s), c, new(0, 0)),
            ];
            ushort[] indices = [
                0, 1, 3, 0, 3, 2,//Front
                10, 11, 9, 10, 9, 8,//Rear
                4, 0, 2, 4, 2, 6,//Left
                5, 7, 3, 5, 3, 1,//Right
                6, 2, 3, 6, 3, 7,//Top
                0, 4, 1, 4, 5, 1,//Bottom
            ];

            return new Mesh(vertices: verts, indices: indices);
        }
    }
}
