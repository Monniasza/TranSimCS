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
        private Texture2D roadTexture; // Assuming you have a texture for the road

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
            // Generate lanes for each node
            Generator.GenerateLanes(2, node1, 3.5f, 0);
            Generator.GenerateLanes(2, node2, 3.5f, 0);
            Generator.GenerateLanes(3, node3, 3.5f, 0);
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
                var laneConnection1 = new LaneConnection(nnode1, nnode2, 0, nnode1.LaneSpecs.Count, 0, nnode2.LaneSpecs.Count);
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
            GraphicsDevice.Clear(Color.ForestGreen);

            
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

                //Calculate lane balances
                int startingLanes = connection.RightStartIndex - connection.LeftStartIndex; // How many lanes are open at the start node
                int endingLanes = connection.RightEndIndex - connection.LeftEndIndex; // How many lanes are open at the start node
                int leftShift = connection.EndShift - connection.StartShift; // How many lanes open to the left (negative means that the left lanes close)
                int rightShift = endingLanes - startingLanes - leftShift; // How many lanes open to the right (negative means that the right lanes close)
                int totalLanes = startingLanes + Math.Abs(leftShift) + Math.Abs(rightShift); // Total lanes after the connection
                int closingLeftLanes = Math.Max(0, -leftShift); // How many lanes close to the left
                int openingLeftLanes = Math.Max(0, leftShift); // How many lanes open to the left
                int closingRightLanes = Math.Max(0, -rightShift); // How many lanes close to the right
                int openingRightLanes = Math.Max(0, rightShift); // How many lanes open to the right

                //Calculate unchanging lanes
                int unchangingLanesStartLeft = connection.LeftStartIndex + closingLeftLanes;
                int unchangingLanesStartRight = connection.RightStartIndex - closingRightLanes;
                int unchangingLanesEndLeft = connection.LeftEndIndex + openingLeftLanes;
                int unchangingLanesEndRight = connection.RightEndIndex - openingRightLanes;
                int unchangingLanesCount = unchangingLanesStartRight - unchangingLanesStartLeft; // How many lanes remain unchanged

                //Draw the changing lanes
                Vector3 pos1IL = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[unchangingLanesStartLeft], connection.StartNode.Azimuth);
                Vector3 pos1IR = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[unchangingLanesStartRight], connection.StartNode.Azimuth);
                Vector3 pos2IL = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[unchangingLanesEndLeft], connection.EndNode.Azimuth);
                Vector3 pos2IR = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[unchangingLanesEndRight], connection.EndNode.Azimuth);

                DrawQuadrilateral(pos2L, pos2IL, pos1IL, pos1L, connection.LaneSpec.Color, roadTexture);
                DrawQuadrilateral(pos2IR, pos2R, pos1R, pos1IR, connection.LaneSpec.Color, roadTexture);

                //Draw the unchanged lanes
                for (int i = 0; i < unchangingLanesCount; i++) {
                    int startLaneIndex = unchangingLanesStartLeft + i; // Calculate the lane index at the start node
                    int endLaneIndex = unchangingLanesEndLeft + i; // Calculate the lane index at the end node
                    DrawLane(startLaneIndex, endLaneIndex, connection);
                }
            });

            //Draw the lane lines
            base.Draw(gameTime);
        }

        private void DrawLane(int laneIndexStart, int laneIndexEnd, LaneConnection connection) {
            // Calculate the position of the lane based on the node's position and the lane index
            Vector3 pos1L = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[laneIndexStart], connection.StartNode.Azimuth);
            Vector3 pos1R = Geometry.calcLineEnd(connection.StartNode.Position, connection.StartNode.PositionOffsets[laneIndexStart+1], connection.StartNode.Azimuth);
            Vector3 pos2L = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[laneIndexEnd], connection.EndNode.Azimuth);
            Vector3 pos2R = Geometry.calcLineEnd(connection.EndNode.Position, connection.EndNode.PositionOffsets[laneIndexEnd+1], connection.EndNode.Azimuth);

            // Draw a quadrilateral representing the lane
            DrawQuadrilateral(pos2L, pos2R, pos1R, pos1L, connection.LaneSpec.Color, roadTexture);
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