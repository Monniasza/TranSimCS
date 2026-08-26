using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;

namespace TranSimCS.Menus.InGame {
    public static class Unprojection {
        public static Ray3 CreatePickRay(
            Vector2 mousePosition,
            Vector2 viewportSize,
            Matrix4x4 view,
            Matrix4x4 projection
        ) {
            var near = Unproject(
                new Vector3(mousePosition, 0),
                Matrix4x4.Identity,
                view,
                projection,
                viewportSize);

            var far = Unproject(
                new Vector3(mousePosition, 1),
                Matrix4x4.Identity,
                view,
                projection,
                viewportSize);

            Vector3 direction = Vector3.Normalize(far - near);

            return new Ray3(near, direction);
        }
        public static Vector3 Unproject(
            Vector3 screen,
            Matrix4x4 world,
            Matrix4x4 view,
            Matrix4x4 projection,
            Vector2 viewportSize
        ) {
            Matrix4x4 wvp = world * view * projection;

            if (!Matrix4x4.Invert(wvp, out var inverse))
                throw new InvalidOperationException("World-view-projection matrix is not invertible.");

            // Window coordinates → NDC
            float x = 2.0f * screen.X / viewportSize.X - 1.0f;
            float y = 1.0f - 2.0f * screen.Y / viewportSize.Y;
            float z = 2.0f * screen.Z - 1.0f;

            Vector4 ndc = new(x, y, z, 1.0f);

            Vector4 worldPosition = Vector4.Transform(ndc, inverse);

            if (MathF.Abs(worldPosition.W) < float.Epsilon)
                throw new InvalidOperationException("Unproject produced an invalid homogeneous coordinate.");

            return new Vector3(
                worldPosition.X / worldPosition.W,
                worldPosition.Y / worldPosition.W,
                worldPosition.Z / worldPosition.W);
        }
    }
}
