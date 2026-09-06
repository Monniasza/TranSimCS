using TranSimCS.ModelOld;
using TranSimCS.SilkNet;

namespace TranSimCS {
    public static class Materials {
        public static TextureData WhiteTex { get; private set; }
        public static TextureData Black { get; private set; }
        public static TextureData GrassTex { get; private set; }

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
        
        public static SimpleMaterial White { get; private set; }
        public static SimpleMaterial WhiteTransparent { get; private set; }

        public static SimpleMaterial MapPin { get; private set; }
        

        public static readonly string CrossIcon = "ui/check";

        //MARKINGS. All emissive
        public static SimpleMaterial Grid { get; private set; }
        public static SimpleMaterial LineYield { get; private set; }
        public static SimpleMaterial LineDash { get; private set; }
        public static SimpleMaterial Impassable { get; private set; }
        public static SimpleMaterial EmissiveWhite { get; private set; }

        //LIGHTS
        public static SimpleMaterial Sun { get; private set; }


        public static void ReadAssets() {
            WhiteTex = TexturePipeline.GetTexture("white.png");
            Black = TexturePipeline.GetTexture("black.png");
            GrassTex = TexturePipeline.GetTexture("seamlessTextures2/grass1.jpg");

            Asphalt = new("seamlessTextures2/IMGP5511_seamless.jpg");

            Grass = new("seamlessTextures2/grass1.jpg");
            //Grass = new("logo1024.png", MaterialBlendMode.Cutout);

            Concrete = new("seamlessTextures2/IMGP5514_seamless_2.jpg");
            Cobble = new("seamlessTextures2/rock02.jpg");
            Tiles = new("pavement.png");
            BuildingBricks = new("brickwall.png");
            BuildingWindows = new("brickwindow.png");
            White = new("white.png");


            WhiteTransparent = new("white.png", MaterialBlendMode.Transparent);
            Road = new("laneTex.png", MaterialBlendMode.Transparent);
            Add = new("addTex.png", MaterialBlendMode.Transparent);

            EmissiveWhite = SimpleMaterial.NewEmissive("white.png");
            Grid = SimpleMaterial.NewEmissive("snapgrid.png", MaterialBlendMode.Cutout);
            LineYield = SimpleMaterial.NewEmissive("lines/yield.png", MaterialBlendMode.Cutout);
            LineDash = SimpleMaterial.NewEmissive("lines/dashed.png", MaterialBlendMode.Cutout);
            Arrow = SimpleMaterial.NewEmissive("markings/arrow.png", MaterialBlendMode.Cutout);
            Impassable = SimpleMaterial.NewEmissive("signs/trafficbarrier.png");

            Sun = new("sun/simple glowing 128px.png", MaterialBlendMode.Transparent);

            MapPin = SimpleMaterial.NewEmissive("navpin.png", MaterialBlendMode.Cutout);
        }
    }
}
