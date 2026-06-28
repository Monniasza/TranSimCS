using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class DemolitionTool(InGameMenu game) : ITool {
        private static Logger log = LogManager.GetCurrentClassLogger();

        string ITool.Name => "Road Demolition Tool";
        string ITool.Description => "Demolish objects and subcomponents";

        public void Draw(GameTime gameTime) {
            var roadSelection = game.MouseOver?.Tag as IRoadElement;

            Mesh renderBin = game.renderHelper.GetOrCreateRenderBinForced(Assets.Road);

            float v1 = 0.2f;
            float v2 = 0.3f;
            var O = new Color(255, 128, 0, 100);
            var R = new Color(255, 0, 0, 100);

            //Determine elements to be removed when left-clicking (the segment/node) in red
            //Determine elements to be removed when right-clicking (the strip/lane) in orange

            var selLane = roadSelection?.GetLane();
            var selStrip = roadSelection?.GetLaneStrip();
            var roadNode = roadSelection?.GetRoadNode();

            //Nodes/lanes
            foreach (var node in game.World.Nodes.data) {
                NodeRenderer.GenerateRoadNodeSelectionMesh(node, renderBin, selLane?.Front, R, O, true);
            }

            if (roadSelection == null) return;
            if (selLane != null && roadNode != null) {
                //Deleting node/lane

                //Segments
                var fw = roadNode.FrontEnd;
                var bw = roadNode.RearEnd;
                var dependencies = new List<RoadStrip>();
                dependencies.AddRange(fw.ConnectedSegments);
                dependencies.AddRange(bw.ConnectedSegments);
                foreach (var dependency in dependencies) {
                    var segmentTag = dependency.FullSizeTag();
                    RoadRenderer.GenerateLaneRangeMesh(segmentTag, renderBin, R, v1);
                }

                //Strips
                var fwe = selLane.Front;
                var bwe = selLane.Rear;
                var laneDependencies = new List<LaneStrip>();
                laneDependencies.AddRange(dependencies.SelectMany(x => x.Lanes).Where(x => (x.StartLane.lane == selLane || x.EndLane.lane == selLane)));
                foreach(var laneDependency in laneDependencies) {
                    var laneTag = laneDependency.Tag();
                    RoadRenderer.GenerateLaneRangeMesh(laneTag, renderBin, O, v2);
                }

                

                //The node/lane itself
                /*
                var nodeQuad = NodeRenderer.GenerateNodeQuad(roadNode, R, v3);
                renderBin.DrawQuad(nodeQuad);
                var lqp = NodeRenderer.GenerateLaneQuad(selLane, Color.Orange, v4);
                renderBin.DrawQuad(lqp);*/
            } else if(selStrip != null) {
                //Deleting segment/strip
                var segment = selStrip.road;
                var segmentTag = segment.FullSizeTag();
                RoadRenderer.GenerateLaneRangeMesh(segmentTag, renderBin, R, v1);
                var stripTag = selStrip.Tag();
                RoadRenderer.GenerateLaneRangeMesh(stripTag, renderBin, O, v2);
            }
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
