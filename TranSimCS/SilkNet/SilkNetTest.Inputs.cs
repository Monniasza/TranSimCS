using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;

namespace TranSimCS.SilkNet {
    public partial class SilkNetTest {
        private void HandleInputs(float dT) {
            var rotationSpeed = 1f;
            var motionSpeed = camera.Distance;
            if(World != null) MouseOver = Selection.CalculateSelection(World.RootIndex, MouseRay);

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
    }
}
