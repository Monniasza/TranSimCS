using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TranSimCS.Roads;

namespace TranSimCS
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private World world;
        private BasicEffect effect;
        public static Texture2D roadTexture { get; private set; } // Assuming you have a texture for the road
        private RenderHelper renderHelper; // Assuming you have a RenderHelper class for rendering

        //private Camera camera; // Assuming you have a Camera class for handling camera logic

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            world = new World();

            //Add some example road nodes and segments
            var node1 = new RoadNode(world, "Node 1", new Vector3(0, 0.1f, 0), RoadNode.AZIMUTH_NORTH);
            var node2 = new RoadNode(world, "Node 2", new Vector3(0, 0.1f, 100), RoadNode.AZIMUTH_NORTH);
            var node3 = new RoadNode(world, "Node 3", new Vector3(0, 0.1f, 200), RoadNode.AZIMUTH_NORTH);
            var node4a = new RoadNode(world, "Node 4a", new Vector3(100, 0.1f, 300), RoadNode.AZIMUTH_EAST);
            var node4b = new RoadNode(world, "Node 4b", new Vector3(0, 0.1f, 300), RoadNode.AZIMUTH_NORTH);
            // Generate lanes for each node
            Generator.GenerateLanes(2, node1, 3.5f, 0);
            Generator.GenerateLanes(2, node2, 3.5f, 0);
            Generator.GenerateLanes(3, node3, 3.5f, 0);
            Generator.GenerateLanes(1, node4a, 3.5f, 0);
            Generator.GenerateLanes(2, node4b, 3.5f, 0);
            world.RoadNodes.Add(node1);
            world.RoadNodes.Add(node2);
            world.RoadNodes.Add(node3);
            world.RoadNodes.Add(node4a);
            world.RoadNodes.Add(node4b);

            //1-2
            var lc12 = Generator.GenerateLaneConnections(node1, 0, node1.Lanes.Count, node2, 0, node2.Lanes.Count);
            world.RoadSegments.Add(lc12);

            //2-3
            var lc23 = Generator.GenerateLaneConnections(node2, 0, node2.Lanes.Count, node3, 0, node3.Lanes.Count);
            world.RoadSegments.Add(lc23);

            //3-4a
            var lc34a = Generator.GenerateLaneConnections(node3, 2, 3, node4a, 0, node4a.Lanes.Count);
            world.RoadSegments.Add(lc34a);

            //3-4b
            var lc34b = Generator.GenerateLaneConnections(node3, 0, 2, node4b, 0, node4b.Lanes.Count);
            world.RoadSegments.Add(lc34b);

            //Generate graphics stuff
            effect = new BasicEffect(GraphicsDevice){
                VertexColorEnabled = true,
                TextureEnabled = true,
                View = Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(new Vector3(0, 32, -64), Vector3.Zero, Vector3.Up),
                World = Matrix.Identity,
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1f, 1000000f),
            };
            renderHelper = new RenderHelper(GraphicsDevice, effect);

            //Load the road texture
            roadTexture = Content.Load<Texture2D>("laneTex");
        }
        public RoadSelection? MouseOverRoad { get; private set; } = null; // Store the selected road selection
        //public RoadSelection? SelectedRoadSelection { get; set; } = null; // Store the selected road selection

        public Ray MouseRay {get; private set; } // Ray from the mouse position in the world

        public Camera camera = new Camera(new Vector3(0, 0, 0), 64, 0, 0.2f); // Initialize the camera
        public float MotionSpeed = 0.3f; // Speed of camera movement
        public float RotationSpeed = 1f; // Speed of camera rotation

        static int scrollWheelValue = 0; // Store the scroll wheel value
        public MouseState LastMouseState { get; private set; } // Store the last mouse state for comparison
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //Pre-get the necessary values for the mouse ray and camera
            MouseState mouseState = Mouse.GetState();
            int mouseX = mouseState.X;
            int mouseY = mouseState.Y;
            Viewport viewport = GraphicsDevice.Viewport;
            KeyboardState keyboardState = Keyboard.GetState();
            float secondsElapsed = (float)gameTime.ElapsedGameTime.TotalSeconds; // Get the elapsed time in seconds

            // Unproject screen coordinates to near and far points in 3D space
            Vector3 nearPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 0), effect.Projection, effect.View, effect.World);
            Vector3 farPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 1), effect.Projection, effect.View, effect.World);
            Ray ray = new(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            MouseRay = ray; // Store the ray for later use

            //Reset the selected lane tag and position
            MouseOverRoad = null; // Reset the selected road selection

            //Road selection logic
            ForeachLane(world.RoadSegments, (lane) => {
                // Check if the ray intersects with the road segment
                object tag = MeshUtil.RayIntersectMesh(lane.GetMesh(), ray, out float intersectionDistance);
                if (tag is LaneStrip laneStrip && laneStrip == lane) {
                    MouseOverRoad = new RoadSelection(laneStrip, intersectionDistance, ray); // Create a new road selection with the lane tag and intersection distance
                }
            });

            //Handle scroll wheel input for zooming in and out
            if (mouseState.ScrollWheelValue != scrollWheelValue) {
                // Zoom in or out based on the scroll wheel value
                int mouseScrollDelta = mouseState.ScrollWheelValue - scrollWheelValue;
                scrollWheelValue = mouseState.ScrollWheelValue; // Update the scroll wheel value
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
            if (mouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released) {
                // If a road segment is selected, remove it from the world
                if (MouseOverRoad != null) {
                    Debug.Print($"Demolishing road segment: {MouseOverRoad.SelectedLaneTag.road}");
                    var tbremove = MouseOverRoad.SelectedLaneTag.road; // Get the road segment to remove
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.Remove(tbremove); // Remove the selected road segment from the world
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (mouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released) {
                // If a lane tag is selected, remove it from the road segment
                if (MouseOverRoad != null) {
                    var selectedRoad = MouseOverRoad.SelectedLaneTag.road; // Get the selected road half
                    var selectedLaneStrip = MouseOverRoad.SelectedLaneStrip; // Get the selected lane tag
                    var selectedRoadHalf = selectedRoad.GetHalf(MouseOverRoad.SelectedRoadHalf);
                    var selectedNode = selectedRoadHalf;// Get the node of the selected road half
                    var selectedLane = selectedLaneStrip.GetHalf(MouseOverRoad.SelectedRoadHalf); // Get the lane number from the selected lane tag 
                    Debug.Print($"Demolishing lane: {selectedLaneStrip} of segment {selectedRoad.StartNode.Id} to {selectedRoad.EndNode.Id}");
                    MouseOverRoad = null; // Reset the mouse over road selection
                    selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                }
            }

            //Refresh the mouse state for the next frame
            base.Update(gameTime);
            LastMouseState = mouseState; // Update the last mouse state for the next frame
        }

        private Color laneHighlightColor = Color.Yellow; // Color for highlighting selected lanes
        private Color laneHighlightColor2 = new Color(0, 192, 255, 100);
        private Color roadSegmentHighlightColor = new Color(0, 128, 255, 100);
        protected override void Draw(GameTime gameTime) {
            //Clear the screen to a solid color and clear the render helper
            GraphicsDevice.Clear(Color.ForestGreen);
            renderHelper.Clear();

            Texture2D testTexture = Content.Load<Texture2D>("test");
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(roadTexture);

            // Draw the asphalt texture for the road
            foreach(var roadSegment in world.RoadSegments) {
                RoadRenderer.RenderRoadSegment(roadSegment, renderBin, 0.001f); // Render each road segment with a slight vertical offset
            }

            //If a road segment is selected, draw the selection
            var roadSelection = MouseOverRoad;
            if(roadSelection != null) {
                // Draw the selected lane tag with a different color
                RoadRenderer.GenerateLaneRangeMesh(roadSelection.SelectedLaneTag, renderBin, laneHighlightColor, 0.005f);
                RoadRenderer.GenerateLaneRangeMesh(roadSelection.SelectedLaneTag.road.FullSizeTag(), renderBin, roadSegmentHighlightColor, 0.002f);
                var splines = RoadRenderer.GenerateSplines(roadSelection.SelectedLaneTag, 0.007f);
                var offset = Vector3.Up * 0.007f; // Offset for the lane position
                Bezier3.Split(splines.Item1, 0.5f, out Bezier3 leftSubBezier1, out Bezier3 leftSubBezier2);
                Bezier3.Split(splines.Item2, 0.5f, out Bezier3 rightSubBezier1, out Bezier3 rightSubBezier2);

                // Draw the left and right bezier curves of the selected lane tag
                if(roadSelection.SelectedLaneT < 0.5f) {
                    RoadRenderer.DrawBezierStrip(leftSubBezier1, rightSubBezier1, renderBin, laneHighlightColor2);
                } else {
                    RoadRenderer.DrawBezierStrip(leftSubBezier2, rightSubBezier2, renderBin, laneHighlightColor2);
                }

                // Draw the position marker for the T value
                Vector3 positionAtT = roadSelection.selectedLaneBezier.Value[roadSelection.SelectedLaneT];
                var SelectedLanePosition = roadSelection.SelectedLanePosition;
            }

            //Red the render helper
            renderHelper.Render();
            base.Draw(gameTime);
        }

        private void ForeachLane(ICollection<RoadStrip> segments, Action<LaneStrip> action){
            foreach (var segment in segments)
                foreach(var lane in segment.Lanes) 
                    action(lane);
                
        }

        private readonly Vector3 v1 = new(-1,  1, 0);
        private readonly Vector3 v2 = new( 1,  1, 0);
        private readonly Vector3 v3 = new( 1, -1, 0);
        private readonly Vector3 v4 = new(-1, -1, 0);
        private void Mark(Vector3 position, Color color, float Size=1) {
            Texture2D testTexture = Content.Load<Texture2D>("test");
            DrawQuadrilateral(
                (v1 * Size) +position, (v2 * Size) +position,
                (v3 * Size) +position, (v4 * Size) +position, color, testTexture);
        }

        private void DrawQuadrilateral(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Texture2D tex){
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(tex);
            renderBin.DrawQuad(a, b, c, d, color);
        }
    }
}