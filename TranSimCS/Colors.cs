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

        public static Rgba32 ToRgba32(this System.Drawing.Color color) => new Rgba32(color.R, color.G, color.B, color.A);
    }
}
