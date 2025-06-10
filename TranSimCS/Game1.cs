using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
            var lc12 = new LaneConnection(node1, node2, 0, node1.LaneSpecs.Count, 0, node2.LaneSpecs.Count);
            world.RoadSegments.Add(lc12);

            //2-3
            var lc23 = new LaneConnection(node2, node3, 0, node2.LaneSpecs.Count, 0, node3.LaneSpecs.Count);
            world.RoadSegments.Add(lc23);

            //3-4a
            var lc34a = new LaneConnection(node3, node4a, 2, 3, 0, node4a.LaneSpecs.Count);
            world.RoadSegments.Add(lc34a);

            //3-4b
            var lc34b = new LaneConnection(node3, node4b, 0, 2, 0, node4b.LaneSpecs.Count);
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

        private LaneTag? SelectedLaneTag = null;
        private Vector3? SelectedLanePosition = null; // Position of the selected lane tag, if any
        private float IntersectionDistance = 0.1f; // Distance to check for intersection with the road segments
        private float SelectedLaneT = 0.5f; // T value for the selected lane tag, if any
        private Ray mouseRay;
        private Bezier3? selectedLaneBezier; // Bezier curve for the selected lane tag
        public Camera camera = new Camera(new Vector3(0, 0, 0), 64, 0, 0.2f); // Initialize the camera
        public float MotionSpeed = 0.3f; // Speed of camera movement


        static int scrollWheelValue = 0; // Store the scroll wheel value
        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Unproject screen coordinates to near and far points in 3D space
            MouseState mouseState = Mouse.GetState();
            int mouseX = mouseState.X;
            int mouseY = mouseState.Y;
            Viewport viewport = GraphicsDevice.Viewport;
            Vector3 nearPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 0), effect.Projection, effect.View, effect.World);
            Vector3 farPoint = viewport.Unproject(new Vector3(mouseX, mouseY, 1), effect.Projection, effect.View, effect.World);
            Ray ray = new(nearPoint, Vector3.Normalize(farPoint - nearPoint));
            mouseRay = ray; // Store the ray for later use

            //Reset the selected lane tag and position
            SelectedLaneTag = null; // Reset the selected lane tag
            SelectedLanePosition = null; // Reset the selected lane position
            IntersectionDistance = float.MaxValue; // Reset the intersection distance

            //Road selector logic can be added here
            foreach (var road in world.RoadSegments) {
                //Select the road segment if the mouse is over it
                foreach (var segment in world.RoadSegments) {
                    // Check if the ray intersects with the road segment
                    object tag = MeshUtil.RayIntersectMesh(segment.StartMesh, ray, out float intersectionDistance);
                    if (tag is LaneTag laneTag && laneTag.road == segment) {
                        // If the ray intersects, mark the road segment as selected
                        SelectedLaneTag = laneTag; // Set the selected lane tag
                        IntersectionDistance = intersectionDistance; // Update the intersection distance
                        SelectedLanePosition = ray.Position + ray.Direction * intersectionDistance; // Store the position of the selected lane tag
                        var splines = RoadRenderer.GenerateSplines(laneTag);
                        Bezier3 averageBezier = (splines.Item1 + splines.Item2) / 2; // Average the two splines
                        selectedLaneBezier = averageBezier; // Store the selected lane bezier curve
                        SelectedLaneT = Bezier3.FindT(averageBezier, SelectedLanePosition.Value); // Get the T value for the selected lane position
                    }
                }                
            }

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

            //Hanle camera movement with WASD keys
            KeyboardState keyboardState = Keyboard.GetState();
            float sideMotion = 0.0f; // Side motion for camera movement
            float forwardMotion = 0.0f; // Forward motion for camera movement
            if (keyboardState.IsKeyDown(Keys.W)) forwardMotion += 1.0f; // Move forward
            if (keyboardState.IsKeyDown(Keys.S)) forwardMotion -= 1.0f; // Move backward
            if (keyboardState.IsKeyDown(Keys.A)) sideMotion -= 1.0f; // Move left
            if (keyboardState.IsKeyDown(Keys.D)) sideMotion += 1.0f; // Move right

            var cameraDirection = camera.GetOffsetVector(); // Get the forward vector of the camera
            var movement = new Vector3(
                cameraDirection.X * forwardMotion + cameraDirection.Z * sideMotion,
                0,
                cameraDirection.Z * forwardMotion - cameraDirection.X * sideMotion
                ) * (float)gameTime.ElapsedGameTime.TotalSeconds * MotionSpeed; // Move forward
            camera.Position += movement; // Update camera position

            Debug.Print($"Camera position: {camera.Position}"); // Print the camera position for debugging

            base.Update(gameTime);
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
            DrawRoadSegments(world.RoadSegments, (connection) => renderBin.DrawModel(connection.StartMesh));

            //If a road segment is selected, draw the selection
            if (SelectedLaneTag != null) {
                // Draw the selected lane tag with a different color
                RoadRenderer.DrawLaneTag(SelectedLaneTag.Value, renderBin, laneHighlightColor, 0.005f);
                RoadRenderer.DrawLaneTag(SelectedLaneTag.Value.road.FullSizeTag(), renderBin, roadSegmentHighlightColor, 0.002f);

                var splines = RoadRenderer.GenerateSplines(SelectedLaneTag.Value, 0.007f);
                var offset = Vector3.Up * 0.007f; // Offset for the lane position
                Bezier3.Split(splines.Item1, SelectedLaneT, out Bezier3 leftSubBezier1, out Bezier3 leftSubBezier2);
                Bezier3.Split(splines.Item2, SelectedLaneT, out Bezier3 rightSubBezier1, out Bezier3 rightSubBezier2);
                // Draw the left and right bezier curves of the selected lane tag
                RoadRenderer.DrawBezierStrip(leftSubBezier2, rightSubBezier2, renderBin, laneHighlightColor2);
            }

            if ( SelectedLanePosition.HasValue) {
                // Draw a marker at the selected lane position
                Mark(SelectedLanePosition.Value, Color.Red, 0.5f);
                
            }

            if (selectedLaneBezier.HasValue) {
                // Draw the selected lane bezier curve
                //Draw the position marker for the T value
                Vector3 positionAtT = selectedLaneBezier.Value[SelectedLaneT];
                Mark(positionAtT, Color.Blue, 0.5f);
            }

            //Red the render helper
            renderHelper.Render();
            base.Draw(gameTime);
        }

        private void DrawRoadSegments(ICollection<LaneConnection> segments, Action<LaneConnection> action){
            foreach (var segment in segments) action(segment);
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