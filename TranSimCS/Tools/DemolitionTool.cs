using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MonoGame.Extended;
using NLog;
using NLog.Time;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class DemolitionTool(InGameMenu game) : ITool {
        private static Logger log = LogManager.GetCurrentClassLogger();

        string ITool.Name => "Road Demolition Tool";
        string ITool.Description => "Demolish objects and subcomponents";

        public void Draw(GameTime gameTime) {
            //Determine elements to be removed when left-clicking (the segment/node) in red
            //Determine elements to be removed when right-clicking (the strip/lane) in orange

            float v1 = 0.2f;
            float v2 = 0.3f;
            var orange = Color.Orange * 0.5f;
            var red = Color.Red * 0.5f;
            Mesh renderBin = game.renderHelper.GetOrCreateRenderBinForced(Assets.Road);
            var roadSelection = game.MouseOver?.Tag as IRoadElement;

            var selLane = roadSelection?.GetLane();
            var selStrip = roadSelection?.GetLaneStrip();
            var roadNode = roadSelection?.GetRoadNode();
            

            var selection = game.MouseOver?.Tag;
            switch (selection) {
                case LaneStrip laneStrip:
                    var segment = laneStrip.Road;
                    var segmentTag = segment.FullSizeTag();
                    RoadRenderer.GenerateLaneRangeMesh(segmentTag, renderBin, red, v1);
                    var stripTag = laneStrip.Tag();
                    RoadRenderer.GenerateLaneRangeMesh(stripTag, renderBin, orange, v2);
                    break;
                case LaneEnd laneEnd:
                    selLane = laneEnd.lane;

                    //Segments
                    var fw = roadNode.FrontEnd;
                    var bw = roadNode.RearEnd;
                    var dependencies = new List<RoadStrip>();
                    dependencies.AddRange(fw.ConnectedSegments);
                    dependencies.AddRange(bw.ConnectedSegments);
                    foreach (var dependency in dependencies) {
                        var segmentTag2 = dependency.FullSizeTag();
                        RoadRenderer.GenerateLaneRangeMesh(segmentTag2, renderBin, red, v1);
                    }
                    //Strips
                    var fwe = selLane.Front;
                    var bwe = selLane.Rear;
                    var laneDependencies = new List<LaneStrip>();
                    laneDependencies.AddRange(dependencies.SelectMany(x => x.Lanes).Where(x => (x.StartLane.lane == selLane || x.EndLane.lane == selLane)));
                    foreach (var laneDependency in laneDependencies) {
                        var laneTag = laneDependency.Tag();
                        RoadRenderer.GenerateLaneRangeMesh(laneTag, renderBin, orange, v2);
                    }
                    break;
                case RoadSection roadSection:
                    var selmesh = roadSection.SelectionMesh.GetMesh().GetOrCreateRenderBinForced(Assets.Asphalt);
                    var whiteBin = game.renderHelper.GetOrCreateRenderBinForced(Assets.WhiteTransparent);
                    var voffset = roadSection.Normal * 0.2f;
                    var txVerts = selmesh.Vertices.Select(x => new VertexPositionColorTexture(x.Position + voffset, red, x.TextureCoordinate)).ToArray();
                    whiteBin.DrawModel(txVerts, selmesh.Indices);
                    break;
            }

            //Nodes/lanes
            foreach (var node in game.World.Nodes.data) {
                NodeRenderer.GenerateRoadNodeSelectionMesh(node, renderBin, selLane?.Front, red, orange, true);
            }

            if (roadSelection == null) return;
        }


        public void Draw2D(GameTime gameTime) {
        }


        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "to demolish the road segment, a node or the entire object"),
            ([MouseButton.Right], "to demolish the lane, a lane strip or a subcomponent")
        ];

        public void Update(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            var MouseOverRoad = game.MouseOver?.Tag as IRoadElement;
            TSWorld world = game.World;

            //Demolish the selected road segment if the left mouse button is clicked
            if (button == MouseButton.Left) {
                // If a road segment is selected, remove it from the world
                var selectedRoad = MouseOverRoad?.GetRoadStrip();
                var selectedNode = MouseOverRoad?.GetRoadNode();
                if (selectedNode != null) {
                    //Demolish a node
                    MouseOverRoad = null;
                    world.Nodes.data.Remove(selectedNode);
                    game.MouseOver = Selection.Invalid;
                } else if (selectedRoad != null) {
                    log.Trace($"Demolishing road segment: {selectedRoad}");
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.data.Remove(selectedRoad); // Remove the selected road segment from the world
                    game.MouseOver = Selection.Invalid;
                } else if(game.MouseOver?.Tag is RoadSection section) {
                    var nodes = section.Nodes.ToArray();
                    foreach (var node in nodes) node.ConnectedSection.Value = null;
                    world.RoadSections.data.Remove(section);
                    game.MouseOver = Selection.Invalid;
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (button == MouseButton.Right) {
                // If a lane tag is selected, remove it from the road segment
                var selectedLaneStrip = MouseOverRoad?.GetLaneStrip();
                var selectedNode = MouseOverRoad?.GetRoadNode();
                var selectedLane = MouseOverRoad?.GetLane();
                if(selectedLane != null) {
                    //Demolish the node lane
                    MouseOverRoad = null; // Reset the mouse over road selection
                    selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                    game.MouseOver = Selection.Invalid;
                } else if(selectedLaneStrip != null) {
                    //Demolish just the lane strip
                    MouseOverRoad = null;
                    selectedLaneStrip.Destroy();
                    game.MouseOver = Selection.Invalid;
                }
            }
        }

        void ITool.AddAttributes(ISet<string> set) {
            set.Add(ToolAttribs.noHighlights);
        }
    }
}
