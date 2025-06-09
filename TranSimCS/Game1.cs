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
            var segment12 = new RoadSegment(world, node1, node2);
            segment12.LaneConnections.Add(lc12);
            world.RoadSegments.Add(segment12);

            //2-3
            var lc23 = new LaneConnection(node2, node3, 0, node2.LaneSpecs.Count, 0, node3.LaneSpecs.Count);
            var segment23 = new RoadSegment(world, node2, node3);
            segment23.LaneConnections.Add(lc23);
            world.RoadSegments.Add(segment23);

            //3-4a
            var lc34a = new LaneConnection(node3, node4a, 2, 3, 0, node4a.LaneSpecs.Count);
            var segment34a = new RoadSegment(world, node3, node4a);
            segment34a.LaneConnections.Add(lc34a);
            world.RoadSegments.Add(segment34a);

            //3-4b
            var lc34b = new LaneConnection(node3, node4b, 0, 2, 0, node4b.LaneSpecs.Count);
            var segment34b = new RoadSegment(world, node3, node4b);
            segment34b.LaneConnections.Add(lc34b);
            world.RoadSegments.Add(segment34b);

            //Generate graphics stuff
            effect = new BasicEffect(GraphicsDevice){
                VertexColorEnabled = true,
                TextureEnabled = true,
                //View = Matrix.CreateLookAt(new Vector3(0, 100, 0), new Vector3(0, 0, -1), Vector3.Backward),
                View = Matrix.CreateScale(-1, 1, 1) * Matrix.CreateLookAt(new Vector3(0, 256, -256), Vector3.Zero, Vector3.Up),
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

            // Draw the asphalt texture for the road
            DrawRoadSegments(world.RoadSegments, (connection) => {
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

                //Draw changing lanes
                DrawTangentialLane(connection.LeftStartIndex, unchangingLanesStartLeft, connection.LeftEndIndex, unchangingLanesEndLeft, connection);
                DrawTangentialLane(unchangingLanesStartRight, connection.RightStartIndex, unchangingLanesEndRight, connection.RightEndIndex, connection);

                //Draw markers
                Vector3 pos1L = Geometry.calcLineEnd(connection.StartNode, connection.LeftStartIndex);
                Vector3 pos1R = Geometry.calcLineEnd(connection.StartNode, connection.RightStartIndex);
                Vector3 pos2L = Geometry.calcLineEnd(connection.EndNode, connection.LeftEndIndex);
                Vector3 pos2R = Geometry.calcLineEnd(connection.EndNode, connection.RightEndIndex);
                Mark(pos1L, Color.White);
                Mark(pos1R, Color.Red);
                Mark(pos2L, Color.Gray);
                Mark(pos2R, Color.Maroon);

                //Draw the unchanged lanes
                for (int i = 0; i < unchangingLanesCount; i++) {
                    int startLaneIndex = unchangingLanesStartLeft + i; // Calculate the lane index at the start node
                    int endLaneIndex = unchangingLanesEndLeft + i; // Calculate the lane index at the end node
                    DrawLane(startLaneIndex, endLaneIndex, connection);
                }
            });

            //Draw the lane lines

            //Red the render helper
            renderHelper.Render();
            base.Draw(gameTime);
        }

        private void DrawLane(int laneIndexStart, int laneIndexEnd, LaneConnection connection) {
            DrawTangentialLane(laneIndexStart, laneIndexStart + 1, laneIndexEnd, laneIndexEnd + 1, connection);
        }

        private void DrawTangentialLane(int laneIndexStartL, int laneIndexStartR, int laneIndexEndL, int laneIndexEndR, LaneConnection connection) {
            var color = connection.LaneSpec.Color; // Get the color from the lane specification

            // Calculate the position of the lane based on the node's position and the lane index
            var pos1L = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartL);
            var pos1R = Geometry.calcLineEnd2(connection.StartNode, laneIndexStartR);
            var pos2L = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndL);
            var pos2R = Geometry.calcLineEnd2(connection.EndNode, laneIndexEndR);

            //Generate border curves
            Vector3[] leftBorder = Geometry.GenerateSplinePoints(pos1L.Position, pos1L.Tangential, pos2L.Position, pos2L.Tangential, 10);
            Vector3[] rightBorder = Geometry.GenerateSplinePoints(pos1R.Position, pos1R.Tangential, pos2R.Position, pos2R.Tangential, 10);
            var leftBorder2 = Geometry.GeneratePositionsFromVectors(0, color, leftBorder);
            var rightBorder2 = Geometry.GeneratePositionsFromVectors(1, color, rightBorder);

            var strip = Geometry.WeaveStrip(leftBorder2, rightBorder2);

            //Draw strip representing the lane
            renderHelper.GetOrCreateRenderBin(roadTexture).DrawStrip(strip); //The strip looks jagged, but it resembles a curve better than a straight line

            // Draw a quadrilateral representing the lane
            //DrawQuadrilateral(pos2L, pos2R, pos1R, pos1L, connection.LaneSpec.Color, roadTexture);
        }

        private void DrawRoadSegments(ICollection<RoadSegment> segments, Action<LaneConnection> action){
            foreach (var segment in segments)
                foreach (var connection in segment.LaneConnections)              
                    // Perform the action on each lane connection
                    action(connection);
        }

        private readonly Vector3 v1 = new(-1,  1, 0);
        private readonly Vector3 v2 = new( 1,  1, 0);
        private readonly Vector3 v3 = new( 1, -1, 0);
        private readonly Vector3 v4 = new(-1, -1, 0);
        private void Mark(Vector3 position, Color color) {
            Texture2D testTexture = Content.Load<Texture2D>("test");
            DrawQuadrilateral(v1+position, v2+position, v3+position, v4+position, color, testTexture);
        }

        private void DrawQuadrilateral(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Texture2D tex){
            renderHelper.GetOrCreateRenderBin(tex).DrawQuad(a, b, c, d, color);
        }
    }
}