using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImageMagick;
using ImGuiNET;
using Microsoft.Xna.Framework.Input;
using NLog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Terrain;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// The Silk.NET-based TranSim window.
    /// </summary>
    public sealed partial class SilkNetTest {
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
        public bool IsMouseOverUI { get; private set; }
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
        public Vector2 MousePosition;
        public Vector2 MousePositionPrev;
        public Ray3 MouseRay;
        public Ray3 MouseRayOld;
        public Selection? MouseOver;

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
                mouse.MouseMove += MouseMove;
            }

            //Create contexts
            OpenGL = SilkWindow.CreateOpenGL();
            RenderManager = new(this);
            ImGuiController = new(OpenGL, SilkWindow, InputContext);

            //Create world data
            camera = new(Vector3.Zero, 32, 1, 0.7f);
        }

        private void MouseMove(IMouse mouse, Vector2 vector) {
            MousePosition = vector;
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

            //Create the pick ray
            MouseRayOld = MouseRay;
            MouseRay = Unprojection.CreatePickRay(MousePosition, new(SilkWindow.Size.X, SilkWindow.Size.Y), RenderManager.View, RenderManager.Projection);
            VectorMethods.CheckVector(MouseRay.Origin, nameof(MouseRay.Origin));
            VectorMethods.CheckVector(MouseRay.Direction, nameof(MouseRay.Direction));

            //Handle picking
            IsMouseOverUI = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            if (!IsMouseOverUI) HandleInputs(dT);

            //Push previous values
            MousePositionPrev = MousePosition;
        }
        private void OnRender(double dt) {
            OpenGL.ClearColor(System.Drawing.Color.CornflowerBlue);
            OpenGL.Clear(ClearBufferMask.ColorBufferBit);
            OpenGL.Clear(ClearBufferMask.DepthBufferBit);

            MultiMesh mesh = new MultiMesh();

            RenderManager.Camera = camera;

            //Render GUI
            DrawUI();

            //Add world contents
            if (World != null) {
                var showNodes = true;
                if (showNodes) foreach (var node in World.Nodes.data) 
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

            //Draw highlights
            Mesh roadRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Road);

            var nodecolor = InGameMenu.roadSegmentHighlightColor;
            var lanecolor = InGameMenu.laneHighlightColor;

            if ((MouseOver?.SelectedObj is RoadStrip strip)) {
                var fstag = strip.Bounds;
                LaneRangeMethods.GenerateLaneRangeMesh(fstag, roadRenderBin, nodecolor, 0.45f);
            }

            //If a road segment is selected, draw the selection
            if ((MouseOver?.Tag) is LaneStrip laneStrip) {
                // Draw the selected lane tag with a different color
                var laneTag = laneStrip.Tag();
                LaneRangeMethods.GenerateLaneRangeMesh(laneTag, roadRenderBin, lanecolor, 0.5f);
            }

            //Draw the selected road node
            var selectedObj = MouseOver?.Tag;
            HalfLane? laneEnd = null;
            if (selectedObj is IRoadElement element && element.GetLaneEnd() != null && element.GetRoadStrip() == null)
                laneEnd = element.GetLaneEnd();

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
