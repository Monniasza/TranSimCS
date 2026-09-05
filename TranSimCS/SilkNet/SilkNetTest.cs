using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using TranSimCS.Render;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.SilkNet.Mode;
using TranSimCS.Terrain;
using TranSimCS.Tools;
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
        public Stats Stats { get; private set; }
        
        //World contents
        public Camera camera;
        public TSWorld World { get; private set; }

        //UI contents
        public bool IsMouseOverUI { get; private set; }
        public readonly List<string> Worlds = [];
        
        public Action? CurrentlyOpenModal;
        
        public bool IsStatsOpen;


        public string SaveTitle = "world.transim";

        public bool AreExamplesOpen;
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

        public SilkNetTest() {
            World = new TSWorld();
            var pickMode = new PickMode(this);
            //Create modes
            AvailableModes = [
                pickMode, new ModeDemolish(this), new ModeNode(this), new ModeSegment(this),
                new ModeSection(this), new ModeConnection(this), new ModeMoveIt(this),
                new ModeReverse(this),
            ];
            _mode = pickMode;
            snappingGrid = new();
            SegmentPresets.RoadMode = RoadModes[2];
        }
        public void Start() {
            try {
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
            }catch(Exception e) {
                log.Fatal("Fatal exception. Quitting the game.");
                log.Fatal(e);
                throw;
            }            
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
                mouse.MouseDown += MouseDown;
                mouse.MouseUp += MouseUp;
            }

            //Create contexts
            OpenGL = SilkWindow.CreateOpenGL();
            RenderManager = new(this);
            RenderManager.OnRender += Render3D;
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

            //Create the pick ray
            MouseRayOld = MouseRay;
            MouseRay = Unprojection.CreatePickRay(MousePosition, new(SilkWindow.Size.X, SilkWindow.Size.Y), RenderManager.View, RenderManager.Projection);
            VectorMethods.CheckVector(MouseRay.Origin, nameof(MouseRay.Origin));
            VectorMethods.CheckVector(MouseRay.Direction, nameof(MouseRay.Direction));

            //Enable/disable selection
            World.RoadSections.trackerSpatial.sceneTree.Active.Value = SelectSections;
            World.RoadSegments.trackerSpatial.sceneTree.Active.Value = SelectSegments;
            World.Nodes.trackerSpatial.sceneTree.Active.Value = SelectNodes;
            World.Cars.trackerSpatial.sceneTree.Active.Value = SelectCars;
            World.TempSelectors.Active.Value = true;

            //Handle picking
            IsMouseOverUI = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            if (!IsMouseOverUI) HandleInputs(dT);
            //Track object positions
            if(TrackPosition != null) {
                camera.Position = TrackPosition.PositionData.Position;
            }

            //Update the tool
            Mode.Update(dt);

            //Update the world
            World.Update(dT);

            //Push previous values
            MousePositionPrev = MousePosition;
            MouseStateOld = MouseState;
        }
        private void OnRender(double dt) {
            Stats stats = default;
            OpenGL.ClearColor(System.Drawing.Color.CornflowerBlue);
            OpenGL.Clear(ClearBufferMask.ColorBufferBit);
            OpenGL.Clear(ClearBufferMask.DepthBufferBit);

            RenderManager.Camera = camera;

            //Render GUI
            DrawUI();
            
            stats.Segments = World.RoadSegments.data.Count;
            stats.Nodes = World.Nodes.data.Count;
            stats.Sections = World.RoadSections.data.Count;
            stats.Cars = World.Cars.data.Count;
            stats.Buildings = World.Buildings.data.Count;

            //Render all
            RenderManager.Render();
            ImGuiController.Render();
            FramesPerSecond.Count++;

            //Apply stats
            var renderStats = RenderManager.Stats;
            stats.Triangles = renderStats.TriangleCount;
            stats.Vertices = renderStats.VertexCount;
            stats.Materials = renderStats.MaterialCount;
            stats.Tags = renderStats.TagCount;
            stats.MeshDraws = renderStats.DrawCount;
            stats.MeshInstances = renderStats.InstanceCount;
            stats.MeshModels = renderStats.ModelCount;
            Stats = stats;
        }
        
    }
}
