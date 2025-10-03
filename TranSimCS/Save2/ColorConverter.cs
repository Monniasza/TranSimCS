using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace TranSimCS.Save2 {
    public class ColorConverter : JsonConverter<Color> {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var oldReader = reader;
            JsonProcessor.ForceRead(ref reader);
            if(reader.TokenType == JsonTokenType.String) {
                string colorString = reader.GetString()!;
                if (colorString.StartsWith("#")) {
                    try {
                        return ParseHexColor(colorString);
                    } catch (Exception e) {
                        JsonProcessor.Fail(reader, "Failed to parse color", e);
                    }
                }
            }
            reader = oldReader;

            byte r = 0, g = 0, b = 0, a = 255;
            JsonProcessor.ReadJsonObjectProperties(ref reader, (ref reader0, key) => {
                reader0.Read();
                switch (key.ToLower()) {
                    case "r":
                        r = reader0.GetByte();
                        break;
                    case "g":
                        g = reader0.GetByte();
                        break;
                    case "b":
                        b = reader0.GetByte();
                        break;
                    case "a":
                        a = reader0.GetByte();
                        break;
                }
            });
            return new Color(r, g, b, a);
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

            throw new FormatException($"Invalid hex color format: {hex}");
        }
    }
}
