using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Input;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    public interface IMode {
        /// <summary>
        /// The concise mode title. Should be short and describe how it works.
        /// </summary>s
        public string Title();
        /// <summary>
        /// The longer mode description. Invoked in the mode information window. Use Dear ImGui.
        /// </summary>
        public void Description() { }

        /// <summary>
        /// Called to draw any mode UIs. TranSim uses Dear ImGui, an immediate mode UI library that does not store UI state.
        /// </summary>
        public void DrawUI() { }
        /// <summary>
        /// Called to draw any visuals for the mode in 3D.
        /// Use <paramref name="renderMeshPool"/> for dynamically generated geometry like highlights
        /// Use <paramref name="target"/> for single, static meshes with fixed contents and dynamic positioning. Accepts non-instanced meshes but is slower with them.
        /// </summary>
        /// <param name="target">render target for instanced meshes</param>
        /// <param name="renderMeshPool">render target for dynamically generated geometry</param>
        public void Draw3D(RenderTarget target, MultiMesh renderMeshPool) { }
        /// <summary>
        /// Called on every game tick.
        /// </summary>
        public void Update(double dt) { }
        /// <summary>
        /// Called when a tool is opened
        /// </summary>
        public void OnOpen() { }
        /// <summary>
        /// Called when a tool is closed
        /// </summary>
        public void OnClose() { }
        /// <summary>
        /// Called when a mouse button is pressed
        /// </summary>
        public void OnMousePress(MouseButton button) { }
        /// <summary>
        /// Called when a mouse button is released
        /// </summary>
        public void OnMouseRelease(MouseButton button) { }
        /// <summary>
        /// Called when a key is pressed
        /// </summary>
        public void OnKeyPress(Key key) { }
        /// <summary>
        /// Called when a key is released
        /// </summary>
        public void OnKeyRelease(Key key) { }
        /// <summary>
        /// Called when a mouse is scrolled
        /// </summary>
        public void OnScroll(ScrollWheel scrollAmount) { }

        public virtual Color SelectionObjectColor() => Colors.SemiClearAzure;
        public virtual Color SelectionComponentColor() => Colors.Yellow;
    }
}
