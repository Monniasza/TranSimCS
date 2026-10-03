using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImageMagick;
using ImGuiNET;
using NLog;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using TranSimCS.Geometry;
using TranSimCS.Mode;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Render;
using TranSimCS.Select;
using TranSimCS.Terrain;
using TranSimCS.Worlds;
using static Schedulers.JobScheduler;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// The Silk.NET-based TranSim window.
    /// </summary>
    public sealed partial class GameWindow {
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

        //ImGui
        private ushort* GlyphRangesPtr;
        private ImFontConfig* _iconFontConfig;
        private ushort* _iconGlyphRanges;
        public ImGuiController ImGuiController { get; private set; }
        private ImFontPtr iconFont;

        public RenderScene RenderContents;

        //Counters
        public FPS FramesPerSecond { get; private set; }
        public FPS TicksPerSecond { get; private set; }
        public Stats Stats { get; private set; }
        
        //World contents
        public ref Camera camera => ref RenderContents.Camera;

        private TSWorld _world;
        public TSWorld World {
            get => _world;
            set {
                _world = value;
                foreach (var tool in AvailableModes) tool.WorldChanged(value);
                TrackPosition = null;
                MouseOver = null;
                Sticky = null;
            }
        }
        public float SimulationSpeed = 1;

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
            Worlds.AddRange(dirInfo.GetFiles().Select(x => Path.GetFileName(x.FullName)));
        }

        //Input attributes
        public Vector2 ScrollOffset;
        public Vector2 MousePosition;
        public Vector2 MousePositionPrev;
        public Ray3 MouseRay;
        public Ray3 MouseRayOld;

        public GameWindow() {
            //Create modes
            AvailableModes = [
                new PickMode(this), new ModeDemolish(this), new ModeNode(this), new ModeSegment(this),
                new ModeSection(this), new ModeConnection(this), new ModeMoveIt(this),
                new ModeReverse(this), new ModeSplit(this), new ModeSplitAt(this), new ModeSpline(this),
                new ModeTrafficLights(this), new ModeScreenshot(this), //new ModeRoadBuilder(this),
            ];
            _mode = AvailableModes[0];
            snappingGrid = new();
            World = new TSWorld();
            SegmentPresets.RoadMode = RoadModes[1];
        }
        public void Start() {
            try {
                WindowOptions options = WindowOptions.Default with {
                    Size = new Vector2D<int>(800, 600),
                    Title = "TranSim",
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
            //Set the window icon
            var windowIcon = TexturePipeline.GetTexture("logo1024.png").ToRawImage();
            SilkWindow.SetWindowIcon(ref windowIcon);

            //Add handlers for inputs
            InputContext = SilkWindow.CreateInput();
            for (int i = 0; i < InputContext.Keyboards.Count; i++) {
                InputContext.Keyboards[i].KeyDown += KeyDown;
                InputContext.Keyboards[i].KeyUp += KeyUp;
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
            RenderContents.SceneGeometry += Render3D;
            RenderContents.Camera = Camera.Default;
            ImGuiController = new(OpenGL, SilkWindow, InputContext, ConfigureImGui);

            //Create world data
            camera = new(Vector3.Zero, 32, 1, 0.7f);
        }

        private void OnClose() {
            FramesPerSecond.Dispose();
            ImGuiController.Dispose();
            InputContext.Dispose();
            OpenGL.Dispose();
            unsafe {
                Marshal.FreeHGlobal((nint)GlyphRangesPtr);
            }
        }
        
        private void OnUpdate(double dt) {
            float dT = (float)dt;

            TicksPerSecond.Count++;
            SilkWindow.Title = $"TranSim. World: {SaveTitle} FPS:{FramesPerSecond.FrameRate}, TPS:{TicksPerSecond.FrameRate}";
            ImGuiController.Update((float)dt);
            ImGuiController.MakeCurrent();

            var wvp = RenderContents.Camera.GetCombinedMatrix(SilkWindow.Size.X, SilkWindow.Size.Y, out _, out _, out _);

            //Create the pick ray
            if (SilkWindow.Size.X > 0 && SilkWindow.Size.Y > 0) {
                MouseRayOld = MouseRay;
                MouseRay = Unprojection.CreatePickRay(MousePosition, new(SilkWindow.Size.X, SilkWindow.Size.Y), wvp, Matrix4x4.Identity);
                VectorMethods.CheckVector(MouseRay.Origin, nameof(MouseRay.Origin));
                VectorMethods.CheckVector(MouseRay.Direction, nameof(MouseRay.Direction));
            }

            //Enable/disable selection
            World.RoadSections.trackerSpatial.sceneTree.Active.Value = SelectSections;
            World.RoadSegments.trackerSpatial.sceneTree.Active.Value = SelectSegments;
            World.Nodes.trackerSpatial.sceneTree.Active.Value = SelectNodes;
            World.Cars.trackerSpatial.sceneTree.Active.Value = SelectCars;
            World.TempSelectors.Active.Value = true;

            //Handle picking
            IsMouseOverUI = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow) || ImGui.GetIO().WantCaptureMouse;
            if (!IsMouseOverUI) HandleInputs(dT);
            //Track object positions
            if(TrackPosition != null) {
                camera.Position = TrackPosition.PositionData.Position;
            }

            //Update the tool
            Mode.Update(dt);

            //Update the world
            if(SimulationSpeed > 0) World.Update(dT * SimulationSpeed);

            //Push previous values
            MousePositionPrev = MousePosition;
            MouseStateOld = MouseState;
        }
        private void OnRender(double dt) {
            Stats stats = default;
            OpenGL.ClearColor(System.Drawing.Color.CornflowerBlue);
            OpenGL.Clear(ClearBufferMask.ColorBufferBit);
            OpenGL.Clear(ClearBufferMask.DepthBufferBit);

            //Render GUI
            ImGui.PushFont(iconFont);
            DrawUI();
            ImGui.PopFont();
            
            stats.Segments = World.RoadSegments.data.Count;
            stats.Nodes = World.Nodes.data.Count;
            stats.Sections = World.RoadSections.data.Count;
            stats.Cars = World.Cars.data.Count;
            stats.Buildings = World.Buildings.data.Count;

            //Set up rendering
            RenderContents.ScreenSize = new(SilkWindow.Size.X, SilkWindow.Size.Y);

            //Render all
            RenderManager.Render(RenderContents);
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

        private unsafe void ConfigureImGui() {
            void VerifyPath(string path) {
                if (!File.Exists(path)) throw new FileNotFoundException($"Font file not found: {path}");
            }

            var io = ImGui.GetIO();

            //var fontPath = Path.Combine(Program.DataRoot, "Files", "fonts", "ARIAL.TTF");
            var fontPath = @"C:\Windows\Fonts\arial.ttf";
            VerifyPath(fontPath);

            var iconPath = Path.Combine(Program.DataRoot, "Files", "fonts", "TranSimIcons.ttf");
            VerifyPath(iconPath);

            // Base font
            var arial = io.Fonts.AddFontFromFileTTF(fontPath, 16.0f, null, io.Fonts.GetGlyphRangesDefault());


            // Keep the native config alive.

            _iconFontConfig = ImGuiNative.ImFontConfig_ImFontConfig();
            _iconFontConfig->MergeMode = 1;
            _iconFontConfig->PixelSnapH = 1;
            _iconFontConfig->GlyphMinAdvanceX = 16;
            _iconFontConfig->GlyphMaxAdvanceX = 16;

            _iconGlyphRanges = (ushort*)Marshal.AllocHGlobal(
                3 * sizeof(ushort));

            _iconGlyphRanges[0] = 0xE000;
            _iconGlyphRanges[1] = 0xF8FF;
            _iconGlyphRanges[2] = 0;

            _iconFontConfig->GlyphRanges = _iconGlyphRanges;

            iconFont = io.Fonts.AddFontFromFileTTF(
                iconPath,
                24.0f,
                _iconFontConfig);
            Debug.Assert(io.Fonts.NativePtr != null, "io.Fonts == null");
            io.Fonts.Build();
            Debug.Assert(io.Fonts.IsBuilt(), "Font atlas not built.");

            var isArialLoaded = arial.IsLoaded();
            if (arial.NativePtr == null || !isArialLoaded)
                throw new Exception($"Failed to load Arial at {fontPath}");
            var isIconFontLoaded = iconFont.IsLoaded();
            if (iconFont.NativePtr == null || !isIconFontLoaded)
                throw new Exception($"Failed to load icon font at {iconPath}");
        }
    }
}
