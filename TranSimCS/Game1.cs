using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            foreach (var node in new[] { node1, node2, node3 })
            {
                // Generate lanes for each node
                Generator.GenerateLanes(2, node, 3.5f, 0);
            }
            world.RoadNodes.Add(node1);
            world.RoadNodes.Add(node2);
            world.RoadNodes.Add(node3);

            // Create a lane connection between the first two nodes
            var segment1 = new RoadSegment(world, node1, node2);
            var segment2 = new RoadSegment(world, node2, node3);
            LaneSpec[] laneSpecs = [LaneSpec.Default, LaneSpec.Truck];
            int i = 0;
            foreach (var segment in new[] { segment1, segment2 }) {
                //Generate connections for the segments
                var laneSpec = laneSpecs[i % laneSpecs.Length];
                var nnode1 = segment.Nodes[0];
                var nnode2 = segment.Nodes[1];
                var laneConnection1 = new LaneConnection(nnode1, nnode2, 0, 1, 0, 1);
                laneConnection1.LaneSpec = laneSpec; // Assign the lane specification to the connection
                segment.LaneConnections.Add(laneConnection1);
                world.RoadSegments.Add(segment);
                i++;
            }

            effect = new BasicEffect(GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = true,
                //View = Matrix.CreateLookAt(new Vector3(0, 100, 0), new Vector3(0, 0, -1), Vector3.Backward),
                View = Matrix.CreateScale(-1, -1, 1) * Matrix.CreateLookAt(new Vector3(0, 16, -64), Vector3.Zero, Vector3.Up),
                World = Matrix.Identity,
                Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1f, 1000f),
            };

            /*EffectTechnique techniques = new(effect.Techniques);
            effect.CurrentTechnique = effect.Techniques["BasicEffect"];*/

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime) {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.ForestGreen);

            Texture2D roadTexture = Content.Load<Texture2D>("laneTex");
            Texture2D testTexture = Content.Load<Texture2D>("test");

            // Draw the asphalt texture for the road
            DrawRoadSegments(world.RoadSegments, (connection) => {
                Vector3 pos1L = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[connection.LeftStartIndex], connection.StartNode.Azimuth);
                Vector3 pos1R = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[connection.RightStartIndex], connection.StartNode.Azimuth);
                Vector3 pos2L = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[connection.LeftEndIndex], connection.EndNode.Azimuth);
                Vector3 pos2R = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[connection.RightEndIndex], connection.EndNode.Azimuth);
                
                Mark(pos1L, Color.White);
                Mark(pos1R, Color.Red);
                Mark(pos2L, Color.Gray);
                Mark(pos2R, Color.Maroon);

                // Draw the road segment texture
                DrawQuadrilateral(pos2L, pos2R, pos1R, pos1L, connection.LaneSpec.Color, roadTexture);
            });

            //DrawQuadrilateral(new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1), Color.Gray, testTexture);
            //DrawQuadrilateral(new(-1, 0, 1), new(1, 0, 1), new(1, 2, 1), new(-1, 2, 1), Color.Green, testTexture);
            //DrawQuadrilateral(new(0, -1, 1), new(2, -1, 1), new(2, 1, 1), new(0, 1, 1), Color.Red, testTexture);
            //DrawQuadrilateral(new(-1, 0, 100), new(1, 0, 100), new(1, 0, 1), new(-1, 0, 1), Color.Blue, testTexture);

            //Draw the lane lines
            base.Draw(gameTime);
        }

        private void DrawRoadSegments(ICollection<RoadSegment> segments, Action<LaneConnection> action){
            foreach (var segment in segments)
                foreach (var connection in segment.LaneConnections)              
                    // Perform the action on each lane connection
                    action(connection);
        }

        private readonly Vector3 v1 = new(-1, -1, 0);
        private readonly Vector3 v2 = new( 1, -1, 0);
        private readonly Vector3 v3 = new( 1,  1, 0);
        private readonly Vector3 v4 = new(-1,  1, 0);
        private void Mark(Vector3 position, Color color) {
            Texture2D testTexture = Content.Load<Texture2D>("test");
            DrawQuadrilateral(v1+position, v2+position, v3+position, v4+position, color, testTexture);
        }


        private static readonly int[] indexData = [0, 1, 2, 0, 2, 3];
        private void DrawQuadrilateral(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Texture2D tex){
            effect.Texture = tex;

            // Draw a quadrilateral using the provided positions and color
            var vertices = new VertexPositionColorTexture[4] {
                new(a, color, new(0, 0)),
                new(b, color, new(1, 0)),
                new(c, color, new(1, 1)),
                new(d, color, new(0, 1)),
            };

            var passes = effect.CurrentTechnique.Passes;
            foreach (EffectPass pass in passes) {//passes are applied, but do not show up in the game window
                pass.Apply();
                GraphicsDevice.DrawUserIndexedPrimitives( //The actual drawing of the quad
                    PrimitiveType.TriangleList, // Specify tri-based quad assembly
                    vertices,                   // Your input vertices
                    0,                          // Offset in vertex array (0 for no offset)
                    4,                          // Length of the input vertices array
                    indexData, // Indicies (a tri with index (0, 1, 2), and (1, 2, 3))
                    0,                          // Offset in index array (0 for none)
                    2                           // Number of tris to draw (2 for a square)
                );
            }
        }
    }
}