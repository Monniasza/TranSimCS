using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Geometry;

namespace TranSimCS.SilkNet {
    public partial class SilkNetTest {
        public static void DrawCompass(float azimuth) {
            var drawList = ImGui.GetForegroundDrawList();

            var displaySize = ImGui.GetIO().DisplaySize;

            float radius = 40;
            Vector2 center = new(
                displaySize.X - radius - 20,
                radius + 20
            );

            // Background
            drawList.AddCircleFilled(
                center,
                radius,
                ImGui.GetColorU32(new Vector4(0, 0, 0, 0.5f))
            );

            drawList.AddCircle(
                center,
                radius,
                ImGui.GetColorU32(Vector4.One)
            );

            DrawDirection(drawList, center, radius, azimuth, "N", 0);
            DrawDirection(drawList, center, radius, azimuth, "E", MathF.PI / 2);
            DrawDirection(drawList, center, radius, azimuth, "S", MathF.PI);
            DrawDirection(drawList, center, radius, azimuth, "W", -MathF.PI / 2);

            //Draw the text
            var angleFormat = azimuth.ToDegrees().ToString("F1");
            DearUI.DrawTextCentered(drawList, angleFormat, center);
        }
        private static void DrawDirection(
            ImDrawListPtr drawList,
            Vector2 center,
            float radius,
            float cameraAzimuth,
            string text,
            float direction
        ) {
            // Compass rotates opposite to camera.
            float angle = direction - cameraAzimuth;

            Vector2 dir = new(
                MathF.Sin(angle),
                -MathF.Cos(angle)
            );

            Vector2 textPos =
                center +
                dir * (radius - 12);

            DearUI.DrawTextCentered(drawList, text, textPos);
        }
    }
}
