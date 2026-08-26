using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using SixLabors.ImageSharp.PixelFormats;
using TranSimCS.Save2;

namespace TranSimCS.Roads {
    public class ColorConverter : JsonConverter<Rgba32> {
        private static Rgba32 ParseHexColor(string hex) {
            int r, g, b, a = 255;

            hex = hex.TrimStart('#');

            switch (hex.Length) {
                case 4:
                    a = int.Parse(hex.Substring(3, 1), NumberStyles.HexNumber);
                    goto case 3;
                case 3:
                    //Short RGB 
                    r = int.Parse(hex.Substring(0, 1), NumberStyles.HexNumber);
                    g = int.Parse(hex.Substring(1, 1), NumberStyles.HexNumber);
                    b = int.Parse(hex.Substring(2, 1), NumberStyles.HexNumber);
                    break;
                case 8:
                    a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                    goto case 6;
                case 6:
                    r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    break;
                default:
                    throw new FormatException($"Invalid hex color format: {hex}");
            }
            return new Rgba32(r, g, b, a);
            
        }

        public override Rgba32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var oldReader = reader;
            reader.Read();
            if (reader.TokenType == JsonTokenType.String) {
                // Handle hex string format
                string? hexString = reader.GetString();
                if (hexString != null) {
                    try {
                        return ParseHexColor(hexString);
                    } catch (Exception e) {
                        JsonProcessor.Fail(reader, "", e);
                    }
                }
            } else if (reader.TokenType == JsonTokenType.StartObject) {
                // Handle object format {R: int, G: int, B: int, A: int}
                int r = 0, g = 0, b = 0, a = 255;
                reader = oldReader;
                JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                    switch (key.ToLower()) {
                        case "r":
                            r = reader0.GetInt32();
                            break;
                        case "g":
                            g = reader0.GetInt32();
                            break;
                        case "b":
                            b = reader0.GetInt32();
                            break;
                        case "a":
                            a = reader0.GetInt32();
                            break;
                    }
                });

                return new Rgba32(r, g, b, a);
            }
            JsonProcessor.Fail(reader, $"Unexpected token type for Color: {reader.TokenType}");
            return Colors.White;
        }

        public override void Write(Utf8JsonWriter writer, Rgba32 value, JsonSerializerOptions options) {
            // Write as hex string for compactness
            string hexString = $"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}";
            writer.WriteStringValue(hexString);
        }
    }
}
