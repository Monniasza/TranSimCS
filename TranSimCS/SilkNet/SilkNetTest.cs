using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using TranSimCS.Terrain;
using DotNet.Collections.Generic;
using TranSimCS.Model;
using TranSimCS.Worlds.Cars;
using SixLabors.ImageSharp;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;

namespace TranSimCS.SilkNet {
    public sealed class SilkNetTest {
        /// <summary>
        /// The Silk.NET window associated with this TranSim window
        /// </summary>
        public IWindow SilkWindow { get; private set; }
        public FPS FramesPerSecond { get; private set; }
        public FPS TicksPerSecond { get; private set; }
        /// <summary>
        /// The OpenGL context associated with this TranSim window
        /// </summary>
        public GL OpenGL { get; private set; }
        public IInputContext InputContext { get; private set; }
        public RenderManager RenderManager { get; private set; }

        public TextureGL CarTex { get; private set; }
        public TextureGL CarEmissive { get; private set; }
        public MultiMesh CarMesh { get; private set; }

        public Camera camera;

        public ImGuiController ImGuiController { get; private set;}

        public void Start() {
            WindowOptions options = WindowOptions.Default with {
                Size = new Vector2D<int>(800, 600),
                Title = "TranSim"
            };
            SilkWindow = Window.Create(options);
            FramesPerSecond = new();
            TicksPerSecond = new();
            
            SilkWindow.Load += OnLoad;
            SilkWindow.Update += OnUpdate;
            SilkWindow.Render += OnRender;
            SilkWindow.FramebufferResize += OnResize;
            //SilkWindow.UpdatesPerSecond = 60;
            //SilkWindow.FramesPerSecond = 60;
            SilkWindow.Closing += OnClose;


            SilkWindow.Run();
        }

        private void OnResize(Vector2D<int> d) {
            OpenGL.Viewport(d);
        }

        private void OnLoad() {
            OpenGL = SilkWindow.CreateOpenGL();
            InputContext = SilkWindow.CreateInput();
            for (int i = 0; i < InputContext.Keyboards.Count; i++) {
                InputContext.Keyboards[i].KeyDown += KeyDown;
                InputContext.Keyboards[i].KeyUp += KeyUp;
                InputContext.Keyboards[i].KeyChar += KeyChar;
            }

            RenderManager = new(this);

            CarMesh = Car.loadedMeshes["synthetic"];

            const string albedoPath = "TranSimCS.Include.car-albedo.png";
            const string emissivePath = "TranSimCS.Include.car-emissive.png";
            CarTex = LoadTextureFromResource(albedoPath);
            CarEmissive = LoadTextureFromResource(emissivePath);

            ImGuiController = new(OpenGL, SilkWindow, InputContext);

            camera = new(Microsoft.Xna.Framework.Vector3.Zero, 20, 1, 0.7f);
        }

        private TextureGL LoadTextureFromResource(string resource) {
            var image = Image.Load(TerrainDataBlobs.OpenEmbeddedResource(resource));
            return new(image, OpenGL);
        }

        private void OnClose() {
            FramesPerSecond.Dispose();
            ImGuiController.Dispose();
            InputContext.Dispose();
            OpenGL.Dispose();
        }

        private void OnUpdate(double dt) {
            TicksPerSecond.Count++;
            SilkWindow.Title = $"TranSim. FPS:{FramesPerSecond.FrameRate}, TPS:{TicksPerSecond.FrameRate}";
            ImGuiController.Update((float)dt);
            ImGuiController.MakeCurrent();
        }
        private void OnRender(double dt) {
            OpenGL.ClearColor(System.Drawing.Color.CornflowerBlue);
            OpenGL.Clear(ClearBufferMask.ColorBufferBit);
            OpenGL.Clear(ClearBufferMask.DepthBufferBit);

            RenderManager.Camera = camera;

            //Render the mesh
            RenderManager.Render(CarMesh);

            //Render GUI
            //ImGuiNET.ImGui.ShowDemoWindow();

            ImGui.Begin("TranSim Options");
            ImGui.Text("Configure TranSim to your liking");
            ImGui.End();

            ImGuiController.Render();

            FramesPerSecond.Count++;
        }
        private void KeyDown(IKeyboard keyboard, Key key, int keyCode) {

        }
        private void KeyUp(IKeyboard keyboard, Key key, int keyCode) {

        }
        private void KeyChar(IKeyboard keyboard, char character) {

        }
    }
}
