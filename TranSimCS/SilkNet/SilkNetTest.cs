using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImageMagick;
using ImGuiNET;
using NLog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using StbImageSharp;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Terrain;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Cars;

namespace TranSimCS.SilkNet {
    public sealed class SilkNetTest {
        //Static contents
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        //Contexts
        /// <summary>
        /// The Silk.NET window associated with this TranSim window
        /// </summary>
        public IWindow SilkWindow { get; private set; }
        /// <summary>
        /// The OpenGL context associated with this TranSim window
        /// </summary>
        public GL OpenGL { get; private set; }
        public IInputContext InputContext { get; private set; }
        public RenderManager RenderManager { get; private set; }
        public ImGuiController ImGuiController { get; private set; }

        //Counters
        public FPS FramesPerSecond { get; private set; }
        public FPS TicksPerSecond { get; private set; }
        
        //World contents
        public Camera camera;
        public TSWorld World { get; private set; }

        //UI contents
        public readonly List<string> Worlds = [];
        public bool IsLoadOpen;
        public void Reload() {
            //Find world files
            var dirInfo = new DirectoryInfo(Program.SaveRoot);
            Worlds.Clear();
            Worlds.AddRange(dirInfo.GetFiles().Select(x => x.FullName));
        }

        //Input attributes
        public Vector2 ScrollOffset;

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
            SilkWindow.Closing += OnClose;
            
            SilkWindow.Run();
        }

        private void MouseScroll(IMouse mouse, ScrollWheel wheel) {
            ScrollOffset.X += wheel.X;
            ScrollOffset.Y += wheel.Y;

            log.Trace($"Mouse scroll delta: {wheel.Y}");
            var zoomDelta = MathF.Pow(2f, -wheel.Y); // Adjust zoom factor based on scroll wheel delta
            camera.Distance *= zoomDelta; // Update camera distance based on zoom factor
            camera.Distance = float.Clamp(camera.Distance, 1, 65536);
        }

        private void OnResize(Vector2D<int> d) {
            OpenGL.Viewport(d);
        }

        private void OnLoad() {
            //Add handlers for inputs
            InputContext = SilkWindow.CreateInput();
            for (int i = 0; i < InputContext.Keyboards.Count; i++) {
                InputContext.Keyboards[i].KeyDown += KeyDown;
                InputContext.Keyboards[i].KeyUp += KeyUp;
                InputContext.Keyboards[i].KeyChar += KeyChar;
            }
            foreach (var mouse in InputContext.Mice) {
                mouse.Scroll += MouseScroll;
            }

            //Create contexts
            OpenGL = SilkWindow.CreateOpenGL();
            RenderManager = new(this);
            ImGuiController = new(OpenGL, SilkWindow, InputContext);

            //Create world data
            camera = new(Vector3.Zero, 32, 1, 0.7f);
        }

        private TextureGPU LoadTextureFromResource(string resource) {
            using var stream = TerrainDataBlobs.OpenEmbeddedResource(resource);
            var image = new MagickImage(stream);
            image.DetermineBitDepth();
            image.DetermineColorType();
            return new(new(image), OpenGL);
        }

        private void OnClose() {
            FramesPerSecond.Dispose();
            ImGuiController.Dispose();
            InputContext.Dispose();
            OpenGL.Dispose();
        }

        private void OnUpdate(double dt) {
            float dT = (float)dt;

            TicksPerSecond.Count++;
            SilkWindow.Title = $"TranSim. FPS:{FramesPerSecond.FrameRate}, TPS:{TicksPerSecond.FrameRate}";
            ImGuiController.Update((float)dt);
            ImGuiController.MakeCurrent();

            var rotationSpeed = 1f;
            var motionSpeed = camera.Distance;

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
            var xVel = motionSpeed * (sinCos.Cos*xz.X + sinCos.Sin*xz.Y);
            var yVel = motionSpeed * (sinCos.Cos*xz.Y - sinCos.Sin*xz.X);

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
        private void OnRender(double dt) {
            OpenGL.ClearColor(System.Drawing.Color.CornflowerBlue);
            OpenGL.Clear(ClearBufferMask.ColorBufferBit);
            OpenGL.Clear(ClearBufferMask.DepthBufferBit);

            MultiMesh mesh = new MultiMesh();

            RenderManager.Camera = camera;



            //Render GUI
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File")) {
                if (ImGui.MenuItem("Load", "", IsLoadOpen, true)) IsLoadOpen ^= true;
                ImGui.EndMenu();
            }
            ImGui.EndMainMenuBar();

            if (IsLoadOpen) {
                ImGui.Begin("Load a world");
                if (ImGui.Button("Reload"))
                    Reload();
                foreach (var world in Worlds) {
                    if (ImGui.Button(world)) {
                        var worldPath = Path.Combine(Program.SaveRoot, world);
                        World = TSWorld.LoadFromFile(worldPath);
                    }
                }
                ImGui.End();
            }

            //Add world contents
            if (World != null) {
                foreach (var node in World.Nodes.data) 
                    mesh.AddAll(node.Mesh.GetMesh());
                foreach (var node in World.RoadSegments.data)
                    mesh.AddAll(node.Mesh.GetMesh());
                foreach (var node in World.RoadSections.data)
                    mesh.AddAll(node.Mesh.GetMesh());
                foreach (var node in World.Buildings.data)
                    mesh.AddAll(node.Mesh.GetMesh());
                foreach (var node in World.Cars.data)
                    mesh.meshInstances.Add(node.meshInstance);
            }
            

            //Add the grass
            Mesh grassMesh = mesh.GetOrCreateRenderBinForced(Assets.Grass);
            InGameMenu.RenderGround(Vector3.Zero, grassMesh);

            //Render all
            RenderManager.Render(mesh);

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
