using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using TranSimCS.Save2;

namespace TranSimCS.Roads {
    public class ColorConverter : JsonConverter<Color> {
        private static Color ParseHexColor(string hex) {
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
                    throw new JsonException($"Invalid hex color format: {hex}");
            }
            return new Color(r, g, b, a);
            
        }

        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var oldReader = reader;
            reader.Read();
            if (reader.TokenType == JsonTokenType.String) {
                // Handle hex string format
                string? hexString = reader.GetString();
                if (hexString != null) {
                    return ParseHexColor(hexString);
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

                return new Color(r, g, b, a);
            }

            throw new JsonException($"Unexpected token type for Color: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            // Write as hex string for compactness
            string hexString = $"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}";
            writer.WriteStringValue(hexString);
        }
    }
}
