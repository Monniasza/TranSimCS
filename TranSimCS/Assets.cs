using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.ModelOld;

namespace TranSimCS {
    public static class Assets {
        public static SimpleMaterial Asphalt { get; private set; }
        public static SimpleMaterial Road { get; private set; }
        public static SimpleMaterial Grass { get; private set; }
        public static SimpleMaterial Add { get; private set; }
        public static SimpleMaterial Concrete { get; private set; }
        public static SimpleMaterial Tiles { get; private set; }
        public static SimpleMaterial Cobble { get; private set; }
        public static SimpleMaterial BuildingBricks { get; private set; }
        public static SimpleMaterial BuildingWindows { get; private set; }
        public static SimpleMaterial Arrow { get; private set; }
        public static Texture2D WhiteTex { get; private set; }
        public static SimpleMaterial White { get; private set; }
        public static SimpleMaterial WhiteTransparent { get; private set; }
        public static SimpleMaterial Grid { get; private set; }
        public static SimpleMaterial LineYield { get; private set; }
        public static SimpleMaterial LineDash { get; private set; }
        public static SimpleMaterial Impassable { get; private set; }

        public static readonly string CrossIcon = "ui/check";

        public static ContentManager Content => Game1.Instance.Content;


        public static void ReadAssets() {
            Asphalt = new("seamlessTextures2/IMGP5511_seamless");
            Road = new("laneTex", MaterialBlendMode.Transparent);
            Grass = new("seamlessTextures2/grass1");
            Add = new("addTex");
            Concrete = new("seamlessTextures2/IMGP5514_seamless_2");
            Cobble = new("seamlessTextures2/rock02");
            Tiles = new("tile");
            BuildingBricks = new("brickwall");
            BuildingWindows = new("brickwindow");
            Arrow = new("markings/arrow", MaterialBlendMode.Cutout);
            WhiteTex = Content.Load<Texture2D>("white");
            White = new("white");
            WhiteTransparent = new("white", MaterialBlendMode.Transparent);
            Grid = new("snapgrid", MaterialBlendMode.Cutout);
            LineYield = new("lines/yield", MaterialBlendMode.Cutout);
            LineDash = new("lines/dashed", MaterialBlendMode.Cutout);
            Impassable = new("signs/trafficbarrier");
        }
    }
}
