using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Clipper2Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MonoGame.Extended.Collections;
using NLog;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Polygons;
using TranSimCS.Roads;
using TranSimCS.Spline;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu : Menu {
        public static readonly Plane groundPlane = new Plane(0, 1, 0, -0.1f);

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public TSWorld World { get; private set; } = null!;
        public readonly Configuration configuration;

        //Graphics
        private BasicEffect effect = null!;
        public RenderHelper renderHelper { get; private set; } = null!; // Assuming you have a RenderHelper class for rendering

        public Ray MouseRay { get; private set; } // Ray from the mouse position in the world
        public Ray MouseRayOld { get; private set; } // Ray from the mouse position in the world


        public float MotionSpeed = 1f; // Speed of camera movement
        public float RotationSpeed = 1f; // Speed of camera rotation
        static int scrollWheelValue = 0; // Store the scroll wheel value

        //UI
        public Panel RootPanel {  get; private set; } = null!;
        public Panel ToolPanel {  get; private set; } = null!;
        public Panel SettingsPanel { get; private set; } = null!;
        public RootElement ToolPanelRoot { get; private set; } = null!;

        public ToolDescriptionPanel ToolDescPanel { get; private set; } = null!;
        public KeyBindPanel KeyBindPanel { get; private set; } = null!;

        public bool IsMouseOverUI { get; private set; }
        public Checkbox CheckNodes { get; private set; } = null!;
        public Checkbox CheckSegments { get; private set; } = null!;
        public Checkbox CheckSections { get; private set; } = null!;

        //Overlays
        public RoadConfigurator configurator { get; private set; }
        public EscapeMenu escapeMenu { get; private set; }

        //Colors
        private Color laneHighlightColor = Color.Yellow; // Color for highlighting selected lanes
        private Color laneHighlightColor2 = new Color(0, 192, 255, 100); //Color for highlighting the selected road half
        private Color roadSegmentHighlightColor = new Color(0, 128, 255, 100); //Color for highlighting selected road segments
        
        //Tools
        public RoadCreationTool RoadCreationTool { get; private set; }

        internal InGameMenu(Game1 game): base(game) {
            configuration = new Configuration();
            configuration.ToolProp.ValueChanged += ToolProp_ValueChanged;
        }

        private void ToolProp_ValueChanged(object? sender, PropertyChangedEventArgs2<ITool?> e) {
            e.OldValue?.OnClose();
            e.NewValue?.OnOpen();
        }

        public override void Destroy() {
            //unused
        }

        public void LoadWorldFromFile(string filename) {
            World = TSWorld.Load(filename);
        }

        public override void LoadContentOverride() {
            if (World == null) {
                World = new TSWorld();
                WorldGenerator.SetUpExampleWorld(World);
            }

            //Generate graphics stuff
            effect = new BasicEffect(Game.GraphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = true
            };
            configuration.Camera.SetUpEffect(effect, this);
            renderHelper = new RenderHelper(Game.GraphicsDevice, effect);

            //Set up meshes
            SelectorObjects = new MultiMesh();

            //Set up the UI from below
            RootPanel = new Panel(MLEM.Ui.Anchor.BottomCenter, new(1, 120));
            ToolPanelRoot = UiSystem.Add("lower", RootPanel);

            ToolPanel = new Panel(MLEM.Ui.Anchor.TopCenter, new(1f, 48));
            RootPanel.AddChild(ToolPanel);

            SettingsPanel = new Panel(MLEM.Ui.Anchor.BottomCenter, new(1f, 40));
            RootPanel.AddChild(SettingsPanel);

            CheckNodes = UI.CreateCheck(this, SettingsPanel, "Select nodes", "ui/node");
            CheckNodes.Checked = true;
            CheckSegments = UI.CreateCheck(this, SettingsPanel, "Select segments", "ui/road");
            CheckSegments.Checked = true;
            CheckSections = UI.CreateCheck(this, SettingsPanel, "Select sections and intersections", "ui/junction");
            CheckSections.Checked = true;

            configurator = new RoadConfigurator(this, configuration.LaneSpecProp, MLEM.Ui.Anchor.Center, new(0.5f, 0.5f));

            RoadCreationTool = new RoadCreationTool(this);
            SetUpToolPictureButton("noTool", null);
            SetUpToolPictureButton("ui/blast2", new RoadDemolitionTool(this));
            SetUpToolPictureButton("addRoadTool", RoadCreationTool);
            SetUpToolPictureButton("addNodeTool", new AddNodeTool(this));
            SetUpToolPictureButton("eyedropper", new PickerTool(this));
            SetUpToolPictureButton("moveTool", new MoveTool(this));
            SetUpToolPictureButton("bucket", new PaintTool(this));
            SetUpToolPictureButton("inspect", new InspectTool(this));
            SetUpToolPictureButton("finish", new RoadFinishTool(this));
            SetUpToolPictureButton("trashdump", new DumpingTool(this));

            //Set up the tool preview
            ToolDescPanel = new ToolDescriptionPanel(this);
            UiSystem.Add("toolDesc", ToolDescPanel);

            KeyBindPanel = new KeyBindPanel(this);
            UiSystem.Add("keybinds", KeyBindPanel);

            //Set up the escape menu
            escapeMenu = new EscapeMenu(this);
        }
        private Image.TextureCallback CreateTextureCallback(Texture2D texture2D) {
            return (_) => new MLEM.Textures.TextureRegion(texture2D);
        }
        private PictureButton SetUpPictureButton(String texture, Action? callback = null) {
            var button = new PictureButton(MLEM.Ui.Anchor.AutoInline, new(40, 40), CreateTextureCallback(Game.Content.Load<Texture2D>(texture)), MLEM.Ui.Anchor.Center, new(32, 32));
            if(callback != null) 
                button.OnPressed = (e) => callback.Invoke();
            ToolPanel.AddChild(button);
            return button;
        }
        private (PictureButton, ITool) SetUpToolPictureButton(String texture, ITool tool) {
            return (SetUpPictureButton(texture, () => configuration.Tool = tool), tool);
        }

        public override void Update(GameTime time) {
            //Pre-get the necessary values for the mouse ray and camera
            int mouseX = Game.MouseState.X;
            int mouseY = Game.MouseState.Y;
            Viewport viewport = Game.GraphicsDevice.Viewport;
            KeyboardState keyboardState = Keyboard.GetState();
            float secondsElapsed = (float)time.ElapsedGameTime.TotalSeconds; // Get the elapsed time in seconds

            //Reset values
            IsMouseOverUI = false;
            MouseOverRoad = null; // Reset the selected road selection

            // Unproject screen coordinates to near and far points in 3D space
            Vector3 nearPoint = Vector3.Zero;
            Vector3 farPoint = Vector3.Zero;
            if (effect != null) {
                nearPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 0), effect.Projection, effect.View, effect.World);
                farPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 1), effect.Projection, effect.View, effect.World);
            }
            VectorMethods.CheckVector(nearPoint, nameof(nearPoint));
            VectorMethods.CheckVector(farPoint, nameof(farPoint));

            var tangential = Vector3.Normalize(farPoint - nearPoint); //this generates NaN values
            VectorMethods.CheckVector(tangential, nameof(tangential));

            Ray ray = new(nearPoint, tangential);
            MouseRayOld = MouseRay;
            MouseRay = ray; // Store the ray for later use

            //Check if mouse is over UI
            foreach (var root in UiSystem.GetRootElements()) {
                var rect = root.Element.Area;
                if (rect.Contains(mouseX, mouseY)) {
                    IsMouseOverUI = true;
                    break;
                }
            }

            CreateSelectors();

            //Remove focus from the toolbar
            ToolPanelRoot?.SelectElement(null);

            if(!UiSystem.IsFocusedOnAny())
                HandleInputs(time, keyboardState, secondsElapsed, ray);

            // Update FPS counter if escape menu is displayed
            if (Overlay == escapeMenu) {
                // Use the previously computed secondsElapsed to calculate FPS safely
                escapeMenu.FpsCounter.Text = $"FPS: {Game.fps.FrameRate:F0} | TPS: {Game.tps.FrameRate}";
            }

            UiSystem.Update(time);
        }

        private void HandleInputs(GameTime time, KeyboardState keyboardState, float secondsElapsed, Ray ray) {
            //Selection logic
            float distance = float.MaxValue;
            object? selection = null;
            if (!IsMouseOverUI) {
                var meshes = World.RootGraph;
                var hovering = meshes.Find(MouseRay, out var selectedNode, out distance, out selection);
            }
            SelectedObject = selection;
            if (selection is LaneStrip laneStrip) {
                MouseOverRoad = new RoadSelection(laneStrip, distance, ray); // Create a new road selection with the lane tag and intersection distance
            }
            if (selection is LaneEnd lane) {
                MouseOverRoad = new RoadSelection(lane, distance, ray);
            }
            if (MouseOverRoad != null) SelectedObject = MouseOverRoad.hitObject;

            //Handle scroll wheel input for zooming in and out
            var camera = configuration.Camera;
            if (Game.MouseState.ScrollWheelValue != scrollWheelValue) {
                // Zoom in or out based on the scroll wheel value
                int mouseScrollDelta = Game.MouseState.ScrollWheelValue - scrollWheelValue;
                scrollWheelValue = Game.MouseState.ScrollWheelValue; // Update the scroll wheel value
                log.Trace($"Mouse scroll delta: {mouseScrollDelta}");
                var zoomDelta = MathF.Pow(2f, mouseScrollDelta / -120f); // Adjust zoom factor based on scroll wheel delta
                camera.Distance *= zoomDelta; // Update camera distance based on zoom factor
            }
            if (effect != null) {
                configuration.Camera.SetUpEffect(effect, this);
            }

            //Handle camera movement with WASD keys
            float sideMotion = 0.0f; // Side motion for camera movement
            float forwardMotion = 0.0f; // Forward motion for camera movement
            float upMotion = 0.0f; // Up motion for camera movement
            if (keyboardState.IsKeyDown(Keys.Space)) upMotion += 1.0f; // Move up
            if (keyboardState.IsKeyDown(Keys.LeftShift)) upMotion -= 1.0f; // Move down
            if (keyboardState.IsKeyDown(Keys.W)) forwardMotion += 1.0f; // Move forward
            if (keyboardState.IsKeyDown(Keys.S)) forwardMotion -= 1.0f; // Move backward
            if (keyboardState.IsKeyDown(Keys.A)) sideMotion -= 1.0f; // Move left
            if (keyboardState.IsKeyDown(Keys.D)) sideMotion += 1.0f; // Move right
            var motionElementX = camera.Distance * MathF.Sin(camera.Azimuth);
            var motionElementZ = camera.Distance * MathF.Cos(camera.Azimuth); // Calculate the motion elements based on the camera's azimuth
            var motionElementY = camera.Distance; // Calculate the vertical motion element based on the camera's elevation
            var movement = new Vector3(
                motionElementX * forwardMotion + motionElementZ * sideMotion,
                upMotion * motionElementY,
                motionElementZ * forwardMotion - motionElementX * sideMotion
                ) * secondsElapsed * MotionSpeed; // Move forward
            camera.Position += movement; // Update camera position

            //Handle camera rotation with arrow keys
            float azimuthMovement = 0.0f; // Movement for camera rotation
            float elevationMovement = 0.0f; // Movement for camera elevation
            if (keyboardState.IsKeyDown(Keys.Left)) azimuthMovement -= 1.0f; // Rotate left
            if (keyboardState.IsKeyDown(Keys.Right)) azimuthMovement += 1.0f; // Rotate right
            if (keyboardState.IsKeyDown(Keys.Up)) elevationMovement += 1f; // Rotate up
            if (keyboardState.IsKeyDown(Keys.Down)) elevationMovement -= 1f; // Rotate down
            float newAzimuth = camera.Azimuth + (azimuthMovement * RotationSpeed * secondsElapsed); // New azimuth for camera rotation
            float newElevation = camera.Elevation + (elevationMovement * RotationSpeed * secondsElapsed); // New elevation for camera rotation
            // Clamp the elevation to prevent flipping the camera upside down
            newElevation = MathHelper.Clamp(newElevation, -MathF.PI / 2 + 0.01f, MathF.PI / 2 - 0.01f);
            camera.Azimuth = newAzimuth; // Update camera azimuth
            camera.Elevation = newElevation; // Update camera elevation
            configuration.Camera = camera;

            //Run the world tool
            var Tool = configuration.Tool;
            if (!IsMouseOverUI) {
                if (Game.MouseState.LeftButton == ButtonState.Pressed && Game.MouseStateOld.LeftButton == ButtonState.Released) Tool?.OnClick(MLEM.Input.MouseButton.Left);
                if (Game.MouseState.MiddleButton == ButtonState.Pressed && Game.MouseStateOld.MiddleButton == ButtonState.Released) Tool?.OnClick(MLEM.Input.MouseButton.Middle);
                if (Game.MouseState.RightButton == ButtonState.Pressed && Game.MouseStateOld.RightButton == ButtonState.Released) Tool?.OnClick(MLEM.Input.MouseButton.Right);
                if (Game.MouseState.XButton1 == ButtonState.Pressed && Game.MouseStateOld.XButton1 == ButtonState.Released) Tool?.OnClick(MLEM.Input.MouseButton.Extra1);
                if (Game.MouseState.XButton2 == ButtonState.Pressed && Game.MouseStateOld.XButton2 == ButtonState.Released) Tool?.OnClick(MLEM.Input.MouseButton.Extra2);
                if (Game.MouseState.LeftButton == ButtonState.Released && Game.MouseStateOld.LeftButton == ButtonState.Pressed) Tool?.OnRelease(MLEM.Input.MouseButton.Left);
                if (Game.MouseState.MiddleButton == ButtonState.Released && Game.MouseStateOld.MiddleButton == ButtonState.Pressed) Tool?.OnRelease(MLEM.Input.MouseButton.Left);
                if (Game.MouseState.RightButton == ButtonState.Released && Game.MouseStateOld.RightButton == ButtonState.Pressed) Tool?.OnRelease(MLEM.Input.MouseButton.Left);
                if (Game.MouseState.XButton1 == ButtonState.Released && Game.MouseStateOld.XButton1 == ButtonState.Pressed) Tool?.OnRelease(MLEM.Input.MouseButton.Left);
                if (Game.MouseState.XButton2 == ButtonState.Released && Game.MouseStateOld.XButton2 == ButtonState.Pressed) Tool?.OnRelease(MLEM.Input.MouseButton.Left);
            }
            foreach (var key in Game.KeyboardState.GetPressedKeys())
                if (Game.KeyboardStateOld.IsKeyUp(key)) {
                    Tool?.OnKeyDown(key);
                    OnKeyDown(key);
                }
            foreach (var key in Game.KeyboardStateOld.GetPressedKeys()) {
                if (Game.KeyboardState.IsKeyUp(key)) Tool?.OnKeyUp(key);
            }
            Tool?.Update(time);
        }

        private Element? _overlay;
        public Element? Overlay { get => _overlay; set{
                if (_overlay == value) return;
                if(_overlay != null) {
                    UiSystem.Remove("configurator");
                }
                _overlay = value;
                if (_overlay != null) UiSystem.Add("configurator", _overlay);
            }
        }
        public void ToggleOverlay(Element overlay) {
            if (overlay == Overlay) Overlay = null;
            else Overlay = overlay;
        }

        private void OnKeyDown(Keys key) {
            switch (key) {
                case Keys.T:
                    ToggleOverlay(configurator);
                    break;
                case Keys.Escape:
                    Overlay = (Overlay == null) ? escapeMenu : null;
                    break;
            }
        }

        public const float minT = 0.3f;
        public const float maxT = 0.7f;
        public override void Draw(GameTime time) {

            //Clear the screen to a solid color and clear the render helper
            renderHelper.Clear();
            
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Road);

            // Draw the asphalt texture for the road
            foreach (var roadSegment in World.RoadSegments) renderHelper.AddAll(roadSegment.Mesh.GetMesh());
            
            //Draw road sections
            foreach (var section in World.RoadSections) renderHelper.AddAll(section.Mesh.GetMesh());

            //Draw buildings
            foreach (var building in World.Buildings.data) renderHelper.AddAll(building.Mesh.GetMesh());

            //If a road segment is selected, draw the selection
            var roadSelection = MouseOverRoad;
            if (roadSelection?.SelectedLaneTag != null) {
                // Draw the selected lane tag with a different color
                var laneRange = roadSelection.SelectedLaneTag.Value;
                RoadRenderer.GenerateLaneRangeMesh(laneRange, renderBin, laneHighlightColor, 0.5f);
                var fstag = laneRange.road.FullSizeTag();
                if(fstag != null) RoadRenderer.GenerateLaneRangeMesh(fstag.Value, renderBin, roadSegmentHighlightColor, 0.45f);
                var splines = RoadRenderer.GenerateSplines(laneRange, 0.55f);
                Bezier3.TriSection(splines.Item1, minT, maxT, out Bezier3 leftSubBezier1, out Bezier3 leftSubBezier2, out Bezier3 leftSubBezier3);
                Bezier3.TriSection(splines.Item2, minT, maxT, out Bezier3 rightSubBezier1, out Bezier3 rightSubBezier2, out Bezier3 rightSubBezier3);

                // Draw the left and right bezier curves of the selected lane tag
                if (roadSelection.SelectedLaneT < minT) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier1, rightSubBezier1, renderBin, laneHighlightColor2);
                } else if (roadSelection.SelectedLaneT < maxT) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier2, rightSubBezier2, renderBin, laneHighlightColor2);
                } else {
                    RoadRenderer.DrawBezierStrip(leftSubBezier3, rightSubBezier3, renderBin, laneHighlightColor2);
                }
            }

            //Draw node selectors
            if (CheckNodes.Checked)
                foreach (var node in World.Nodes.data)
                    renderHelper.AddAll(node.Mesh.GetMesh());

            //Draw SelectorObjects
            renderHelper.AddAll(SelectorObjects);

            //Draw the selected road node
            if (roadSelection?.SelectedLaneEnd != null && roadSelection.SelectedLaneStrip == null) {
                //Lane selected, road strip not
                var lane = roadSelection.SelectedLaneEnd.Value;
                var quad = RoadRenderer.GenerateLaneQuad(lane, 0.5f, Color.Yellow);
                var nodeQuad = RoadRenderer.GenerateRoadNodeSelQuad(lane.lane.RoadNode, roadSegmentHighlightColor, 0.45f);
                renderBin.DrawQuad(quad);
                renderBin.DrawQuad(nodeQuad);
            }

            //If the add lane button is selected, draw it
            IRenderBin plusRenderBin = renderHelper.GetOrCreateRenderBinForced(Assets.Add);
            if (SelectedObject is AddLaneSelection selection)
                RoadRenderer.CreateAddLane(selection, plusRenderBin, configuration.LaneSpec.Width, roadSegmentHighlightColor, 0.5f);

            //Render ground with multiple planes
            var centerPos = configuration.Camera.Position;
            IRenderBin grassBin = renderHelper.GetOrCreateRenderBinForced(Assets.Grass);
            RenderGround(centerPos, grassBin);

            //Render road tool
            configuration.Tool?.Draw(time);

            //Render the render helper
            var tris = 0;
            var verts = 0;
            foreach (var bin in renderHelper.RenderBins.Values) {
                tris += (bin.Indices.Count) / 3;
                verts += bin.Vertices.Count;
            }
            renderHelper.Render();
        }

        private void RenderGround(Vector3 posoffset, IRenderBin renderBin) {
            posoffset.Y = 0;

            //Render the center
            GroundParallelogram(renderBin, posoffset, new(-1, 0, -1), Vector3.UnitX * 2, Vector3.UnitZ * 2, 1000);

            //Render concentric rings, each 2 times bigger
            float scale = 1000;
            Vector3[] basisVectors = new Vector3[] {
                Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitX
            };

            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < 4; j++) {
                    var prevVector = basisVectors[j];
                    var nextVector = basisVectors[j + 1];
                    GroundParallelogram(renderBin, posoffset, -(nextVector+(prevVector*2)), prevVector, nextVector * 3, scale);
                }
                scale *= 2;
            }
        }
        private void GroundParallelogram(IRenderBin renderBin, Vector3 initialpos, Vector3 basepos, Vector3 xplus, Vector3 yplus, float scale) {
            var a = (initialpos + basepos * scale);
            var s = scale / 100;
            var C = Color.White;
            var xmul = xplus * scale;
            var ymul = yplus * scale;
            var b = a + ymul;
            var c = b + xmul;
            var d = a + xmul;
            renderBin.DrawQuad(
                GenerateGroundVertex(a, s, C),
                GenerateGroundVertex(b, s, C),
                GenerateGroundVertex(c, s, C),
                GenerateGroundVertex(d, s, C)
            );
        }
        private VertexPositionColorTexture GenerateGroundVertex(Vector3 pos, float texscale, Color? color = null) {
            var c = color ?? Color.White;
            return new VertexPositionColorTexture(pos, c, new(pos.X / texscale, pos.Z / texscale));
        }

        public (object[], string)[] FixedKeys() => [
            ([Keys.Escape], "Pause menu"),
            ([Keys.W, Keys.A, Keys.S, Keys.D], "Move"),
            ([Keys.T], "Edit lane specs"),
            ([Keys.Left, Keys.Right, Keys.Up, Keys.Down], "Rotate the camera")
        ];

        private (object[], string)[] lastDescription;
        public override void Draw2D(GameTime time) {
            var Tool = configuration.Tool;
            ToolDescPanel.Update();
            KeyBindPanel.Update();

            Tool?.Draw2D(time);

            UiSystem.Draw(time, Game.SpriteBatch);
        }
    }
}
