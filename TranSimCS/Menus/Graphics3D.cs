using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Model;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Menus {
    public class Graphics3D {
        public readonly GraphicsDevice Device;
        public readonly BasicEffect Effect;
        public readonly Property<Camera> CameraProp;
        public readonly RenderHelper RenderHelper;
        public Camera Camera { get => CameraProp.Value; set => CameraProp.Value = value; }

        public Graphics3D(GraphicsDevice gpu, Property<Camera>? prop = null) {
            Device = gpu;
            Effect = new BasicEffect(gpu);
            CameraProp = prop ?? new Property<Camera>(Camera.Default, "camera");
            RenderHelper = new RenderHelper(gpu);

            CameraProp.ValueChanged += CameraProp_ValueChanged;
        }

        private void CameraProp_ValueChanged(object? sender, PropertyChangedEventArgs2<Camera> e) {
            Camera.SetUpEffect(Effect, Device);
        }

        public void Render() {
            RenderHelper.Render();
        }
    }
}
