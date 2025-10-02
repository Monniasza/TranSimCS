using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace TranSimCS.Save2 {
    public class ColorConverter : JsonConverter<Color> {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType == JsonTokenType.String) {
                // Support hex color format like "#RRGGBBAA"
                string colorString = reader.GetString()!;
                if (colorString.StartsWith("#")) {
                    return ParseHexColor(colorString);
                }
            }

            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException("Expected StartObject or String token");
            }

            byte r = 0, g = 0, b = 0, a = 255;

            while (reader.Read()) {
                if (reader.TokenType == JsonTokenType.EndObject) {
                    return new Color(r, g, b, a);
                }

                if (reader.TokenType == JsonTokenType.PropertyName) {
                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName.ToLower()) {
                        case "r":
                            r = reader.GetByte();
                            break;
                        case "g":
                            g = reader.GetByte();
                            break;
                        case "b":
                            b = reader.GetByte();
                            break;
                        case "a":
                            a = reader.GetByte();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON");
        }

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
            // Write as hex string for compactness
            writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}{value.A:X2}");
        }

        private static Color ParseHexColor(string hex) {
            hex = hex.TrimStart('#');
            
            if (hex.Length == 6) {
                // RGB format
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                return new Color(r, g, b);
            } else if (hex.Length == 8) {
                // RGBA format
                byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                byte a = Convert.ToByte(hex.Substring(6, 2), 16);
                return new Color(r, g, b, a);
            }

            throw new JsonException($"Invalid hex color format: {hex}");
        }
    }
}
