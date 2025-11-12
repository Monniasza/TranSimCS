using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Model {
    public class RenderManager {
        public readonly Property<Camera> CameraProp;
        public readonly GraphicsDevice gpu;
        public readonly BasicEffect effect;

        public RenderManager(GraphicsDevice gpu) {
            this.gpu = gpu;
            CameraProp = new(default, "camera", null);
            effect = new BasicEffect(gpu);
            Camera.SetUpEffect(effect, gpu);
            CameraProp.ValueChanged += CameraProp_ValueChanged;
        }

        private void CameraProp_ValueChanged(object? sender, PropertyChangedEventArgs2<Camera> e) {
            Camera.SetUpEffect(effect, gpu);
        }

        public Camera Camera { get => CameraProp.Value; set => CameraProp.Value = value; }
    }
}
