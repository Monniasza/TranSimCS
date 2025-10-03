using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TranSimCS {
    public static class Assets {
        public static Texture2D Asphalt { get; private set; }
        public static Texture2D Road { get; private set; }
        public static Texture2D Grass { get; private set; }
        public static Texture2D Add { get; private set; }
        public static ContentManager Content => Game1.Instance.Content;


        public static void ReadAssets() {
            var content = Game1.Instance.Content;
            Asphalt = content.Load<Texture2D>("seamlessTextures2/IMGP5511_seamless");
            Road = content.Load<Texture2D>("laneTex");
            Grass = Content.Load<Texture2D>("seamlessTextures2/grass1");
            Add = Game.Content.Load<Texture2D>("addTex");
        }
    }
}
