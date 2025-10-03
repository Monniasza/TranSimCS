using System;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
using TranSimCS.Save;

namespace TranSimCS.Roads {
    public class ColorConverter : JsonConverter<Color> {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.String) {
                // Handle hex string format
                string? hexString = reader.Value?.ToString();
                if (hexString != null) {
                    return ParseHexColor(hexString);
                }
            } else if (reader.TokenType == JsonToken.StartObject) {
                // Handle object format {R: int, G: int, B: int, A: int}
                int r = 0, g = 0, b = 0, a = 255;
                
                JsonProcessor.ReadJsonObjectProperties(reader, key => {
                    switch (key.ToLower()) {
                        case "r":
                            r = reader.ReadAsInt32() ?? 0;
                            break;
                        case "g":
                            g = reader.ReadAsInt32() ?? 0;
                            break;
                        case "b":
                            b = reader.ReadAsInt32() ?? 0;
                            break;
                        case "a":
                            a = reader.ReadAsInt32() ?? 255;
                            break;
                    }
                });
                
                return new Color(r, g, b, a);
            }
            
            throw new JsonException($"Unexpected token type for Color: {reader.TokenType}");
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer) {
            // Write as hex string for compactness
            string hexString = $"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}";
            writer.WriteValue(hexString);
        }

        private static Color ParseHexColor(string hex) {
            hex = hex.TrimStart('#');
            
            if (hex.Length == 6) {
                // RGB format
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                return new Color(r, g, b, 255);
            } else if (hex.Length == 8) {
                // RGBA format
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                int a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                return new Color(r, g, b, a);
            }
            
            throw new JsonException($"Invalid hex color format: {hex}");
        }
    }
}
