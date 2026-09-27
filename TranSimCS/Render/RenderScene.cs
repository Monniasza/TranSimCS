using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.SilkNet;

namespace TranSimCS.Render {
    /// <summary>
    /// Contains the scene description needed to draw the scene
    /// </summary>
    public struct RenderScene {
        public GeometrySupplier? SceneGeometry;
        public Camera Camera;
        public Size ScreenSize;
        public Vector4 AmbientColor;
        public uint RenderTargetHandle;
    }
}
