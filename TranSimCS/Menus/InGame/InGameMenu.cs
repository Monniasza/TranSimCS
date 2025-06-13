using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Ui.Elements;
using TranSimCS.Roads;

namespace TranSimCS.Menus.InGame {
    public class InGameMenu : Menu {
        private World world;
        public ITool Tool {  get; set; }

        //Graphics
        private BasicEffect effect;
        private RenderHelper renderHelper; // Assuming you have a RenderHelper class for rendering

        //Textures
        public static Texture2D roadTexture { get; private set; } // Assuming you have a texture for the road
        public static Texture2D testTexture { get; private set; }
        public static Texture2D grassTexture {  get; private set; }

        //Inputs      
        public RoadSelection? MouseOverRoad { get; private set; } = null; // Store the selected road selection
        public RoadSelection? SelectedRoadSelection { get; set; } = null; // Store the selected road selection

        public Ray MouseRay { get; private set; } // Ray from the mouse position in the world
        public Camera camera = new Camera(new Vector3(0, 0, 0), 64, 0, 0.2f); // Initialize the camera
        public float MotionSpeed = 0.3f; // Speed of camera movement
        public float RotationSpeed = 1f; // Speed of camera rotation
        static int scrollWheelValue = 0; // Store the scroll wheel value
        public MouseState LastMouseState => Game.MouseStateOld; // Store the last mouse state for comparison

        //UI
        public Panel rootPanel {  get; private set; }
        public bool IsMouseOverUI { get; private set; }

        
        private Color laneHighlightColor = Color.Yellow; // Color for highlighting selected lanes
        private Color laneHighlightColor2 = new Color(0, 192, 255, 100);
        private Color roadSegmentHighlightColor = new Color(0, 128, 255, 100);
        

        internal InGameMenu(Game1 game): base(game) {

        }

        public override void Destroy() {
            throw new NotImplementedException();
        }

        public override void LoadContent() {
            world = new World();
            World.SetUpExampleWorld(world);

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
            roadTexture = Game.Content.Load<Texture2D>("laneTex");
            testTexture = Game.Content.Load<Texture2D>("test");
            grassTexture = Game.Content.Load<Texture2D>("seamlessTextures2/grass1");

            //Set up the UI from below
            rootPanel = new Panel(MLEM.Ui.Anchor.BottomCenter, new(1, 100), false, true);
            UiSystem.Add("lower", rootPanel);

            SetUpPictureButton("noTool", () => Tool = null);

            var AddRoadButton = new PictureButton(MLEM.Ui.Anchor.AutoInline, new(64, 64), CreateTextureCallback(Game.Content.Load<Texture2D>("addRoadTool")));
            rootPanel.AddChild(AddRoadButton);

            var RemoveRoadButton = new PictureButton(MLEM.Ui.Anchor.AutoInline, new(64, 64), CreateTextureCallback(Game.Content.Load<Texture2D>("removeRoadTool")));
            rootPanel.AddChild(RemoveRoadButton);

        }
        private Image.TextureCallback CreateTextureCallback(Texture2D texture2D) {
            return (_) => new MLEM.Textures.TextureRegion(texture2D);
        }
        private PictureButton SetUpPictureButton(String texture, Action? callback = null) {
            var button = new PictureButton(MLEM.Ui.Anchor.AutoInline, new(64, 64), CreateTextureCallback(Game.Content.Load<Texture2D>("removeRoadTool")));
            if(callback != null) 
                button.OnPressed = (e) => callback.Invoke();
            rootPanel.AddChild(button);
            return button;
        }

        public override void Update(GameTime time) {
            //Pre-get the necessary values for the mouse ray and camera
            int mouseX = Game.MouseState.X;
            int mouseY = Game.MouseState.Y;
            Viewport viewport = Game.GraphicsDevice.Viewport;
            KeyboardState keyboardState = Keyboard.GetState();
            float secondsElapsed = (float)time.ElapsedGameTime.TotalSeconds; // Get the elapsed time in seconds

            //Check if mouse is over UI
            IsMouseOverUI = false;
            foreach(var root in UiSystem.GetRootElements()) {
                var rect = root.Element.Area;
                if(rect.Contains(mouseX, mouseY)) {
                    IsMouseOverUI = true;
                    break;
                }
            }

            // Unproject screen coordinates to near and far points in 3D space
            Vector3 nearPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 0), effect.Projection, effect.View, effect.World);
            Vector3 farPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 1), effect.Projection, effect.View, effect.World);
            Ray ray = new(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            MouseRay = ray; // Store the ray for later use

            //Reset the selected lane tag and position
            MouseOverRoad = null; // Reset the selected road selection

            //Road selection logic
            if(!IsMouseOverUI) ForeachLane(world.RoadSegments, (lane) => {
                // Check if the ray intersects with the road segment
                object tag = MeshUtil.RayIntersectMesh(lane.GetMesh(), ray, out float intersectionDistance);
                if (tag is LaneStrip laneStrip && laneStrip == lane) {
                    MouseOverRoad = new RoadSelection(laneStrip, intersectionDistance, ray); // Create a new road selection with the lane tag and intersection distance
                }
            });

            //Handle scroll wheel input for zooming in and out
            if (Game.MouseState.ScrollWheelValue != scrollWheelValue) {
                // Zoom in or out based on the scroll wheel value
                int mouseScrollDelta = Game.MouseState.ScrollWheelValue - scrollWheelValue;
                scrollWheelValue = Game.MouseState.ScrollWheelValue; // Update the scroll wheel value
                Debug.Print($"Mouse scroll delta: {mouseScrollDelta}");
                var zoomDelta = MathF.Pow(2f, mouseScrollDelta / -120f); // Adjust zoom factor based on scroll wheel delta
                camera.Distance *= zoomDelta; // Update camera distance based on zoom factor
            }
            effect.View = camera.GetViewMatrix(); // Update the view matrix of the effect with the camera's view matrix

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

            //Demolish the selected road segment if the left mouse button is clicked
            if (Game.MouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released) {
                // If a road segment is selected, remove it from the world
                if (MouseOverRoad != null) {
                    Debug.Print($"Demolishing road segment: {MouseOverRoad.SelectedLaneTag.road}");
                    var tbremove = MouseOverRoad.SelectedLaneTag.road; // Get the road segment to remove
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.Remove(tbremove); // Remove the selected road segment from the world
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (Game.MouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released) {
                // If a lane tag is selected, remove it from the road segment
                if (MouseOverRoad != null) {
                    var selectedRoad = MouseOverRoad.SelectedLaneTag.road; // Get the selected road half
                    var selectedLaneStrip = MouseOverRoad.SelectedLaneStrip; // Get the selected lane tag
                    var selectedNode = selectedRoad.GetHalf(MouseOverRoad.SelectedRoadHalf);// Get the node of the selected road half
                    var selectedLane = selectedLaneStrip.GetHalf(MouseOverRoad.SelectedRoadHalf); // Get the lane number from the selected lane tag 
                    if (MouseOverRoad.SelectedLaneT > 0.3f && MouseOverRoad.SelectedLaneT < 0.7f) {
                        //Demolish just the lane strip
                        Debug.Print($"Demolishing lane strip: {selectedLaneStrip} of segment {selectedRoad.StartNode.Id} to {selectedRoad.EndNode.Id}");
                        MouseOverRoad = null;
                        selectedLaneStrip.Destroy();
                    } else {
                        //Demolish the node lane
                        Debug.Print($"Demolishing lane: {selectedLane} of segment {selectedRoad.StartNode.Id} to {selectedRoad.EndNode.Id}");
                        MouseOverRoad = null; // Reset the mouse over road selection
                        selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                    }
                }
            }
        }
        public override void Draw(GameTime time) {
            //Clear the screen to a solid color and clear the render helper
            renderHelper.Clear();

            Texture2D testTexture = Game.Content.Load<Texture2D>("test");
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(roadTexture);

            // Draw the asphalt texture for the road
            foreach (var roadSegment in world.RoadSegments) {
                RoadRenderer.RenderRoadSegment(roadSegment, renderBin, 0.001f); // Render each road segment with a slight vertical offset
            }

            //If a road segment is selected, draw the selection
            var roadSelection = MouseOverRoad;
            if (roadSelection != null) {
                // Draw the selected lane tag with a different color
                RoadRenderer.GenerateLaneRangeMesh(roadSelection.SelectedLaneTag, renderBin, laneHighlightColor, 0.005f);
                RoadRenderer.GenerateLaneRangeMesh(roadSelection.SelectedLaneTag.road.FullSizeTag(), renderBin, roadSegmentHighlightColor, 0.002f);
                var splines = RoadRenderer.GenerateSplines(roadSelection.SelectedLaneTag, 0.007f);
                var offset = Vector3.Up * 0.007f; // Offset for the lane position
                Bezier3.TriSection(splines.Item1, 0.3f, 0.7f, out Bezier3 leftSubBezier1, out Bezier3 leftSubBezier2, out Bezier3 leftSubBezier3);
                Bezier3.TriSection(splines.Item2, 0.3f, 0.7f, out Bezier3 rightSubBezier1, out Bezier3 rightSubBezier2, out Bezier3 rightSubBezier3);

                // Draw the left and right bezier curves of the selected lane tag
                if (roadSelection.SelectedLaneT < 0.3f) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier1, rightSubBezier1, renderBin, laneHighlightColor2);
                } else if (roadSelection.SelectedLaneT < 0.7f) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier2, rightSubBezier2, renderBin, laneHighlightColor2);
                } else {
                    RoadRenderer.DrawBezierStrip(leftSubBezier3, rightSubBezier3, renderBin, laneHighlightColor2);
                }
            }

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

            //Red the render helper
            renderHelper.Render();
        }
        private void ForeachLane(ICollection<RoadStrip> segments, Action<LaneStrip> action) {
            foreach (var segment in segments)
                foreach (var lane in segment.Lanes)
                    action(lane);

        }

        public override void Draw2D(GameTime time) {
            string toolName = (Tool?.Name) ?? "no tool";
            Game.SpriteBatch.Begin();
            Game.SpriteBatch.DrawString(Game.Font, toolName, new(25, 25), Color.Gray);
            Game.SpriteBatch.End();

            UiSystem.Draw(time, Game.SpriteBatch);
        }
    }
}
