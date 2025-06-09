using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                //View = Matrix.CreateLookAt(new Vector3(0, 1000, 0), new Vector3(0, 0, -1), Vector3.Backward),
                //View = Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(new Vector3(0, 256, -256), Vector3.Zero, Vector3.Up),
                View = Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(new Vector3(0, 32, -64), Vector3.Zero, Vector3.Up),
                World = Matrix.Identity,
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1f, 1000f),
            };
            renderHelper = new RenderHelper(GraphicsDevice, effect);

            //Load the road texture
            roadTexture = Content.Load<Texture2D>("laneTex");
        }

        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            //Clear the screen to a solid color and clear the render helper
            GraphicsDevice.Clear(Color.ForestGreen);
            renderHelper.Clear();

            Texture2D testTexture = Content.Load<Texture2D>("test");
            IRenderBin renderBin = renderHelper.GetOrCreateRenderBin(roadTexture);

            // Draw the asphalt texture for the road
            DrawRoadSegments(world.RoadSegments, (connection) => renderBin.DrawModel(connection.StartMesh));

            //Draw the lane lines

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