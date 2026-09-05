namespace TranSimCS {
    public static class Colors {
        public static Color SmokedGlass = Color.Black.AlphaMul(0.5f);
        public static Color SemiClearGray = Gray.AlphaMul(0.5f);
        public static Color SemiClearAzure = new Color(0, 160, 255).AlphaMul(0.5f);
        public static Color SemiClearWhite = Color.White.AlphaMul(0.5f);
        public static Color SemiClearRed = new Color(255, 0, 0, 128);
        public static Color White = new Color(255, 255, 255, 255);
        public static Color Red = new Color(255, 0, 0, 255);
        public static Color Maroon = new Color(128, 0, 0, 255);
        public static Color Gray = new(128, 128, 128, 255);
        public static Color LightGray = new(204, 204, 204, 255);
        public static Color DarkGray = new(64, 64, 64, 255);
        public static Color Transparent = new(0, 0, 0, 0);
        public static Color LightGoldenrodYellow = new(255, 204, 128);
        public static Color Green = new(0, 255, 0);
        public static Color LightYellow = new(255, 255, 128);
        public static Color Yellow = new(255, 255, 0);
        public static Color Cyan = new(0, 255, 255);
        public static Color Magenta = new(255, 0, 255);
        public static Color SkyBlue = new(135, 206, 235);
        public static Color DeepSkyBlue = new(0, 191, 155);

        public static Color Orange = new(255, 160, 0);


        public static Color ToColor(this System.Drawing.Color color) => new Color(color.R, color.G, color.B, color.A);

        public static Color ToColor(this Color color) => new Color(color.R, color.G, color.B, color.A);

        public static Color AlphaMul(this Color color, float alpha) => new Color(color.R, color.G, color.B, (byte)(color.A * alpha));
    }
}
