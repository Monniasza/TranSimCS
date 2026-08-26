using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp.PixelFormats;

namespace TranSimCS {
    public static class Colors {
        public static Color SmokedGlass = Color.Black * 0.5f;
        public static Color SemiClearGray = Color.Gray * 0.5f;
        public static Color SemiClearAzure = new Color(0, 160, 255) * 0.5f;
        public static Color SemiClearWhite = Color.White * 0.5f;
        public static Rgba32 White = new Rgba32(255, 255, 255, 255);
        public static Rgba32 Red = new Rgba32(255, 0, 0, 255);
        public static Rgba32 Maroon = new Rgba32(128, 0, 0, 255);
        public static Rgba32 Gray = new(128, 128, 128, 255);
        public static Rgba32 LightGray = new(204, 204, 204, 255);
        public static Rgba32 DarkGray = new(64, 64, 64, 255);
        public static Rgba32 Transparent = new(0, 0, 0, 0);
        public static Rgba32 LightGoldenrodYellow = new(255, 204, 128);
        public static Rgba32 Green = new(0, 255, 0);
        public static Rgba32 LightYellow = new(255, 255, 128);
        public static Rgba32 Yellow = new(255, 255, 0);
        public static Rgba32 Cyan = new(0, 255, 255);
        public static Rgba32 Magenta = new(255, 0, 255);
        public static Rgba32 SkyBlue = new(135, 206, 235);
        public static Rgba32 DeepSkyBlue = new(191, 255, 0);


        public static Color ToMonoGame(this Rgba32 c) => new(c.Rgba);
        public static Rgba32 ToRgba32(this System.Drawing.Color color) => new Rgba32(color.R, color.G, color.B, color.A);

        public static Rgba32 ToRgba32(this Color color) => new Rgba32(color.R, color.G, color.B, color.A);

        public static Rgba32 AlphaMul(this Rgba32 color, float alpha) => new Rgba32(color.R, color.G, color.B, (byte)(color.A * alpha));
    }
}
