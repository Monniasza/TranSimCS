using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;

namespace TranSimCS.SilkNet {
    public partial class SilkNetTest {

        private void MouseScroll(IMouse mouse, ScrollWheel wheel) {
            ScrollOffset.X += wheel.X;
            ScrollOffset.Y += wheel.Y;

            if (IsMouseOverUI) return;
            log.Trace($"Mouse scroll delta: {wheel.Y}");
            var zoomDelta = MathF.Pow(2f, -wheel.Y); // Adjust zoom factor based on scroll wheel delta
            camera.Distance *= zoomDelta; // Update camera distance based on zoom factor
            camera.Distance = float.Clamp(camera.Distance, 1, 65536);

            Mode.OnScroll(wheel);
            
        }
        private void HandleInputs(float dT) {
            var rotationSpeed = 1f;
            var motionSpeed = camera.Distance;
            MouseOver = Selection.CalculateSelection(World.RootIndex, MouseRay);

            //Handle movement
            Vector2 xz = Vector2.Zero;
            Vector2 yawPitch = Vector2.Zero;
            if (ImGui.IsKeyDown(ImGuiKey.W)) xz.Y += 1;
            if (ImGui.IsKeyDown(ImGuiKey.S)) xz.Y -= 1;
            if (ImGui.IsKeyDown(ImGuiKey.A)) xz.X -= 1;
            if (ImGui.IsKeyDown(ImGuiKey.D)) xz.X += 1;
            if (ImGui.IsKeyDown(ImGuiKey.LeftArrow)) yawPitch.X -= 1;
            if (ImGui.IsKeyDown(ImGuiKey.RightArrow)) yawPitch.X += 1;
            if (ImGui.IsKeyDown(ImGuiKey.UpArrow)) yawPitch.Y += 1;
            if (ImGui.IsKeyDown(ImGuiKey.DownArrow)) yawPitch.Y -= 1;

            if (xz != Vector2.Zero) TrackPosition = null;

            var sinCos = MathF.SinCos(camera.Azimuth);
            var xVel = motionSpeed * (sinCos.Cos * xz.X + sinCos.Sin * xz.Y);
            var yVel = motionSpeed * (sinCos.Cos * xz.Y - sinCos.Sin * xz.X);

            float newElevation = camera.Elevation + yawPitch.Y * rotationSpeed * dT;
            float newAzimuth = camera.Azimuth + yawPitch.X * rotationSpeed * dT;
            newElevation = GeometryUtils.Clamp(newElevation, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);
            var newX = camera.Position.X + xVel * dT;
            var newY = camera.Position.Y;
            var newZ = camera.Position.Z + yVel * dT;

            camera.Position = new(newX, newY, newZ);
            camera.Elevation = newElevation;
            camera.Azimuth = newAzimuth;
        }

        private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {
            if (ImGui.IsAnyItemFocused()) return;
            Mode.OnKeyPress(key);
        }
        private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {
            if (ImGui.IsAnyItemFocused()) return;
            Mode.OnKeyRelease(key);
        }
        private void KeyChar(IKeyboard keyboard, char character) {

        }
        private void MouseDown(IMouse mouse, MouseButton button) {
            if (IsMouseOverUI) return;
            Mode.OnMousePress(button);
            
        }
        private void MouseUp(IMouse mouse, MouseButton button) {
            if (IsMouseOverUI) return;
            Mode.OnMouseRelease(button);
        }

        private void MouseMove(IMouse mouse, Vector2 vector) {
            MousePosition = vector;
        }
    }
}
