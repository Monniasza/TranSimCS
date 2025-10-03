using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using NLog;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public partial class InGameMenu : Menu {
        public static readonly Plane groundPlane = new Plane(0, 1, 0, -0.1f);

        private static Logger log = LogManager.GetCurrentClassLogger();

        public TSWorld World { get; private set; } = null!;

        private ITool? _tool;
        public ITool? Tool {
            get => _tool;
            set {
                var newTool = value;
                _tool?.OnClose();
                newTool?.OnOpen();
                _tool = value;
            }
        }

        //Graphics
        private BasicEffect effect = null!;
        public RenderHelper renderHelper { get; private set; } = null!; // Assuming you have a RenderHelper class for rendering

        //Inputs
        public RoadSelection? MouseOverRoad { get; set; } = null; // Store the selected road selection
        public object? SelectedObject = null;
        public Vector3 IntersectWithGround(Ray ray) {
            return Geometry.IntersectRayPlane(ray, groundPlane);
        }
        public Vector3 GroundSelection => IntersectWithGround(MouseRay);
        public Vector3 GroundSelectionOld => IntersectWithGround(MouseRayOld);

        public Ray MouseRay { get; private set; } // Ray from the mouse position in the world
        public Ray MouseRayOld { get; private set; } // Ray from the mouse position in the world
        public Camera camera = new Camera(new Vector3(0, 0, 0), 64, 0, 0.2f); // Initialize the camera
        public float MotionSpeed = 1f; // Speed of camera movement
        public float RotationSpeed = 1f; // Speed of camera rotation
        static int scrollWheelValue = 0; // Store the scroll wheel value

        //UI
        public Panel RootPanel {  get; private set; } = null!;
        public Panel ToolPanel {  get; private set; } = null!;
        public Panel SettingsPanel { get; private set; } = null!;
        public RootElement ToolPanelRoot { get; private set; } = null!;

        public Panel ToolDescPanel { get; private set; } = null!;
        public Panel KeyBindPanel { get; private set; } = null!;
        public Paragraph ToolName {  get; private set; } = null!;
        public Paragraph ToolDesc {  get; private set; } = null!;
        public bool IsMouseOverUI { get; private set; }
        public Checkbox CheckNodes { get; private set; } = null!;
        public Checkbox CheckSegments { get; private set; } = null!;
        public Checkbox CheckSections { get; private set; } = null!;
        public Property<LaneSpec> roadProperty { get; private set; }

        //Overlays
        public RoadConfigurator configurator { get; private set; }
        public EscapeMenu escapeMenu { get; private set; }

        //In-world UI
        public MultiMesh SelectorObjects { get; private set; }
        public Mesh InvisibleSelectors { get; private set; }

        //Colors
        private Color laneHighlightColor = Color.Yellow; // Color for highlighting selected lanes
        private Color laneHighlightColor2 = new Color(0, 192, 255, 100); //Color for highlighting the selected road half
        private Color roadSegmentHighlightColor = new Color(0, 128, 255, 100); //Color for highlighting selected road segments
        
        //Tools
        public RoadCreationTool RoadCreationTool { get; private set; }

        internal InGameMenu(Game1 game): base(game) {
            roadProperty = new Property<LaneSpec>(LaneSpec.Default, "lane spec");
        }

        public override void Destroy() {
            //unused
        }

        public void LoadWorldFromFile(string filename) {
            World = new TSWorld();
            World.ReadFromFile(filename);
        }

        public override void LoadContent() {
            if (World == null) {
                World = new TSWorld();
                WorldGenerator.SetUpExampleWorld(World);
            }

            //Generate graphics stuff
            effect = new BasicEffect(Game.GraphicsDevice) {
                VertexColorEnabled = true,
                TextureEnabled = true,
                View = Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(new Vector3(0, 32, -64), Vector3.Zero, Vector3.Up),
                World = Matrix.Identity,
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, Game.GraphicsDevice.Viewport.AspectRatio, 1f, 1000000f),
            };
            renderHelper = new RenderHelper(Game.GraphicsDevice, effect);

            //Load the road texture

            //Set up meshes
            SelectorObjects = new MultiMesh();
            InvisibleSelectors = new Mesh();

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

            configurator = new RoadConfigurator(this, roadProperty, MLEM.Ui.Anchor.Center, new(0.5f, 0.5f));

            RoadCreationTool = new RoadCreationTool(this);
            SetUpToolPictureButton("noTool", null);
            SetUpToolPictureButton("ui/blast2", new RoadDemolitionTool(this));
            SetUpToolPictureButton("addRoadTool", RoadCreationTool);
            SetUpToolPictureButton("addNodeTool", new AddNodeTool(this));
            SetUpToolPictureButton("eyedropper", new PickerTool(this));
            SetUpToolPictureButton("moveTool", new MoveTool(this));
            SetUpToolPictureButton("bucket", new PaintTool(this));

            //Set up the tool preview
            ToolDescPanel = new Panel(MLEM.Ui.Anchor.TopLeft, new(0.5f, 20), true);
            UiSystem.Add("tooldesc", ToolDescPanel);

            KeyBindPanel = new Panel(MLEM.Ui.Anchor.TopRight, new(0.5f, 40), true);
            UiSystem.Add("keybinds", KeyBindPanel);

            ToolName = new Paragraph(MLEM.Ui.Anchor.AutoInline, 1, "");
            ToolName.RegularFont = Game.Gsf;
            ToolDescPanel.AddChild(ToolName);
            ToolDesc = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 1, "");
            ToolDescPanel.AddChild(ToolDesc);

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
            return (SetUpPictureButton(texture, () => Tool = tool), tool);
        }

        public override void Update(GameTime time) {
            //Pre-get the necessary values for the mouse ray and camera
            int mouseX = Game.MouseState.X;
            int mouseY = Game.MouseState.Y;
            Viewport viewport = Game.GraphicsDevice.Viewport;
            KeyboardState keyboardState = Keyboard.GetState();
            float secondsElapsed = (float)time.ElapsedGameTime.TotalSeconds; // Get the elapsed time in seconds

            // Unproject screen coordinates to near and far points in 3D space
            Vector3 nearPoint = Vector3.Zero;
            Vector3 farPoint = Vector3.Zero;
            if (effect != null) {
                nearPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 0), effect.Projection, effect.View, effect.World);
                farPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 1), effect.Projection, effect.View, effect.World);
            }
            Ray ray = new(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            MouseRayOld = MouseRay;
            MouseRay = ray; // Store the ray for later use

            //Check if mouse is over UI
            IsMouseOverUI = false;
            foreach (var root in UiSystem.GetRootElements()) {
                var rect = root.Element.Area;
                if (rect.Contains(mouseX, mouseY)) {
                    IsMouseOverUI = true;
                    break;
                }
            }

            //Reset the selected lane tag and position
            MouseOverRoad = null; // Reset the selected road selection

            //Add road node selection meshes
            var meshes = new List<IRenderBin>();
            if (World != null) {
                if (CheckSegments.Checked) ForeachLane(World.RoadSegments, (lane) => {
                    meshes.Add(lane.GetMesh());
                });
                if (CheckNodes.Checked) foreach (var node in World.RoadNodes)
                        meshes.Add(node.GetMesh());
            }

            //Add tool selectors
            SelectorObjects.Clear();
            Tool?.AddSelectors(SelectorObjects);
            foreach (var mesh in SelectorObjects.RenderBins.Values)
                meshes.Add(mesh);
            meshes.Add(InvisibleSelectors);

            //Remove focus from the toolbar
            ToolPanelRoot?.SelectElement(null);

            if(!UiSystem.IsFocusedOnAny())
                HandleInputs(time, keyboardState, secondsElapsed, ray, meshes);

            UiSystem.Update(time);
        }

        private void HandleInputs(GameTime time, KeyboardState keyboardState, float secondsElapsed, Ray ray, List<IRenderBin> meshes) {
            //Selection logic
            float distance = float.MaxValue;
            object? selection = null;
            if (!IsMouseOverUI) selection = MeshUtil.RayIntersectMeshes(meshes, ray, out distance);
            SelectedObject = selection;
            if (selection is LaneStrip laneStrip) {
                MouseOverRoad = new RoadSelection(laneStrip, distance, ray); // Create a new road selection with the lane tag and intersection distance
            }
            if (selection is LaneEnd lane) {
                MouseOverRoad = new RoadSelection(lane, distance, ray);
            }
            if (MouseOverRoad != null) SelectedObject = MouseOverRoad.hitObject;

            //Handle scroll wheel input for zooming in and out
            if (Game.MouseState.ScrollWheelValue != scrollWheelValue) {
                // Zoom in or out based on the scroll wheel value
                int mouseScrollDelta = Game.MouseState.ScrollWheelValue - scrollWheelValue;
                scrollWheelValue = Game.MouseState.ScrollWheelValue; // Update the scroll wheel value
                log.Trace($"Mouse scroll delta: {mouseScrollDelta}");
                var zoomDelta = MathF.Pow(2f, mouseScrollDelta / -120f); // Adjust zoom factor based on scroll wheel delta
                camera.Distance *= zoomDelta; // Update camera distance based on zoom factor
            }
            if (effect != null) {
                effect.View = camera.GetViewMatrix(); // Update the view matrix of the effect with the camera's view matrix
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
            var cameraDirection = camera.GetOffsetVector(); // Get the forward vector of the camera
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

            //Run the world tool
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

            Texture2D testTexture = Game.Content.Load<Texture2D>("test");
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(roadTexture);

            // Draw the asphalt texture for the road
            foreach (var roadSegment in World.RoadSegments) {
                RoadRenderer.RenderRoadSegment(roadSegment, renderBin, 0.001f); // Render each road segment with a slight vertical offset
            }

            //Draw road node meshes
            if(CheckNodes.Checked) foreach(var roadNode in World.RoadNodes)
                renderBin.DrawModel(roadNode.GetMesh());

            //Draw road sections
            IRenderBin asphaltBin = renderHelper.GetOrCreateRenderBin(asphaltTexture);
            foreach (var section in World.RoadSections) {
                asphaltBin.DrawModel(section.GetMesh());
            }

            //Draw the tool mesh
            renderHelper.AddAll(SelectorObjects);

            //If a road segment is selected, draw the selection
            var roadSelection = MouseOverRoad;
            if (roadSelection?.SelectedLaneTag != null) {
                // Draw the selected lane tag with a different color
                var laneRange = roadSelection.SelectedLaneTag.Value;
                RoadRenderer.GenerateLaneRangeMesh(laneRange, renderBin, laneHighlightColor, 0.005f);
                RoadRenderer.GenerateLaneRangeMesh(laneRange.road.FullSizeTag(), renderBin, roadSegmentHighlightColor, 0.002f);
                var splines = RoadRenderer.GenerateSplines(laneRange, 0.007f);
                var offset = Vector3.Up * 0.007f; // Offset for the lane position
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

            //Draw the selected road node
            if(roadSelection?.SelectedLaneEnd != null && roadSelection.SelectedLaneStrip == null) {
                //Lane selected, road strip not
                var lane = roadSelection.SelectedLaneEnd.Value;
                var quad = RoadRenderer.GenerateLaneQuad(lane, 0.005f, Color.Yellow);
                var nodeQuad = RoadRenderer.GenerateRoadNodeSelQuad(lane.lane.RoadNode, roadSegmentHighlightColor, 0.002f);
                renderBin.DrawQuad(quad);
                renderBin.DrawQuad(nodeQuad);
            }

            //If the add lane button is selected, draw it
            IRenderBin plusRenderBin = renderHelper.GetOrCreateRenderBin(addTexture);
            if (SelectedObject is AddLaneSelection selection) 
                RoadRenderer.CreateAddLane(selection, plusRenderBin, roadProperty.Value.Width, roadSegmentHighlightColor, 0.002f);
            
            //Render the ground (now just a flat plane)
            float r = 100000;
            float s = 10000;
            IRenderBin grassBin = renderHelper.GetOrCreateRenderBin(grassTexture);
            grassBin.DrawQuad(
                new VertexPositionColorTexture(new(-r, 0,  r), Color.White, new(-s, -s)),
                new VertexPositionColorTexture(new( r, 0,  r), Color.White, new( s, -s)),
                new VertexPositionColorTexture(new( r, 0, -r), Color.White, new( s,  s)),
                new VertexPositionColorTexture(new(-r, 0, -r), Color.White, new(-s,  s))
            );

            //Render road tool
            Tool?.Draw(time);

            //Render the render helper
            renderHelper.Render();
        }
        private void ForeachLane(ICollection<RoadStrip> segments, Action<LaneStrip> action) {
            foreach (var segment in segments)
                foreach (var lane in segment.Lanes)
                    action(lane);
        }

        private (object[], string)[] PromptKeys() => [
            ([Keys.Escape], "Pause menu"),
            ([Keys.W, Keys.A, Keys.S, Keys.D], "Move"),
            ([Keys.T], "Edit lane specs"),
            ([Keys.Left, Keys.Right, Keys.Up, Keys.Down], "Rotate the camera")
        ];

        private (object[], string)[] lastDescription;
        public override void Draw2D(GameTime time) {
            string toolName = (Tool?.Name) ?? "no tool";
            string toolDesc = (Tool?.Description) ?? "";
            Game.SpriteBatch.Begin();
            ToolName.Text = toolName;
            ToolDesc.Text = toolDesc;

            //Handle keybinds
            var keylist = new List<(object[], string)>();
            keylist.AddRange(PromptKeys());
            var k2 = Tool?.PromptKeys();
            if (k2 != null) keylist.AddRange(k2);
            var keybinds = keylist.ToArray();

            var keybindsChanged = !Equality.DeepArrayEqualsWithNull(lastDescription, keybinds);

            if (keybindsChanged) {
                log.Trace("Refreshing keybinds: ");
                log.Trace(keybinds);
                //Keybinds changed
                KeyBindPanel.RemoveChildren();
                foreach(var keybind in keybinds ?? []) {
                    var keys = keybind.Item1;
                    bool firstInLine = true;
                    foreach(var key in keys) {
                        var tex = KeyPromptMapper.GetPrompt(key);
                        if (tex != null) {
                            Image img = new Image(firstInLine ? MLEM.Ui.Anchor.AutoLeft : MLEM.Ui.Anchor.AutoInline, new(1, 1), tex, true);
                            KeyBindPanel.AddChild(img);
                            firstInLine = false;
                        }
                    }
                    var desc = keybind.Item2;
                    var paragraph = new Paragraph(MLEM.Ui.Anchor.AutoInline, 0.5f, desc ?? "", true);
                    KeyBindPanel.AddChild(paragraph);
                }
                KeyBindPanel.SetAreaDirty();
            }
            lastDescription = keybinds;

            ToolDescPanel.SetAreaDirty();
            Game.SpriteBatch.End();
            Tool?.Draw2D(time);

            UiSystem.Draw(time, Game.SpriteBatch);
        }
    }
}
