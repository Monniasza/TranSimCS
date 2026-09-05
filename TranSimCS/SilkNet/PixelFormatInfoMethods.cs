using System;
using System.Runtime.InteropServices;
using ImageMagick;

namespace TranSimCS.SilkNet {
    public static class PixelFormatInfoMethods {
        public static byte[] DumpPixelData(this MagickImage image, TextureFormat tf) {
            using var pixels = image.GetPixelsUnsafe();

            byte[] bytedata = tf switch {
                TextureFormat.R16 => 
                    MemoryMarshal.AsBytes(pixels.ToShortArray("R").AsSpan()).ToArray(),
                TextureFormat.RG16 => 
                    MemoryMarshal.AsBytes(pixels.ToShortArray("RA").AsSpan()).ToArray(),
                TextureFormat.RGB16 => 
                    MemoryMarshal.AsBytes(pixels.ToShortArray("RGB").AsSpan()).ToArray(),
                TextureFormat.RGBA16 =>
                    MemoryMarshal.AsBytes(pixels.ToShortArray("RGBA").AsSpan()).ToArray(),
                TextureFormat.RGB8 => 
                    pixels.ToByteArray("RGB"),
                TextureFormat.RGBA8 =>
                    pixels.ToByteArray("RGBA"),
                _ => throw new ArgumentException("Invalid TextureFormat", nameof(tf))
            } ?? throw new BadImageFormatException("Conversion failed");

            return bytedata;
        }
        public static TextureFormat GetPreferredFormat(this MagickImage image) {
            //Find the optimal pixel format
            bool hasAlpha = image.HasAlpha;
            bool isGray = image.ColorType is ColorType.Grayscale or ColorType.GrayscaleAlpha;

            if (isGray)
                return hasAlpha
                    ? TextureFormat.RG16
                    : TextureFormat.R16;

            if (image.Depth > 8)
                return hasAlpha
                    ? TextureFormat.RGBA16
                    : TextureFormat.RGB16;

            return hasAlpha
                ? TextureFormat.RGBA8
                : TextureFormat.RGB8;


            var colorType = image.ColorType;

            //Surely simulation formats
            if (colorType == ColorType.Grayscale) return TextureFormat.R16;
            if (colorType == ColorType.GrayscaleAlpha) return TextureFormat.RG16;

            //Can be either simulation or visual
            if(colorType == ColorType.TrueColor) {
                //Either RGB16 or RGB8
                if (image.Depth > 8) return TextureFormat.RGB16;
                return TextureFormat.RGB8;
            }
            if(colorType == ColorType.TrueColorAlpha) {
                //Either RGBA16 or RGBA8
                if (image.Depth > 8) return TextureFormat.RGBA16;
                return TextureFormat.RGBA8;
            }

            //These formats are surely visual
            if (colorType is ColorType.ColorSeparation or ColorType.Palette or ColorType.Bilevel) return TextureFormat.RGB8;
            if (colorType is ColorType.PaletteAlpha or ColorType.PaletteBilevelAlpha or ColorType.ColorSeparationAlpha or ColorType.Optimize or ColorType.Undefined) return TextureFormat.RGBA8;

            //Guard against faulty code
            throw new InvalidOperationException($"Unhandled Magick.NET ColorType: {colorType}");
        }
    }
}
