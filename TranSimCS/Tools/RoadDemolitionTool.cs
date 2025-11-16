using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MonoGame.Extended;
using NLog;
using NLog.Time;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class RoadDemolitionTool(InGameMenu game) : ITool {

        private static Logger log = LogManager.GetCurrentClassLogger();
        string ITool.Name => "Road Demolition Tool";

        string ITool.Description => "Demolish objects and subcomponents";

        public void Draw(GameTime gameTime) {
            var roadSelection = game.MouseOverRoad;
            if (roadSelection == null) return;

            Mesh renderBin = game.renderHelper.GetOrCreateRenderBinForced(Assets.Road);

            float v1 = 0.2f;
            float v2 = 0.3f;
            float v3 = 0.55f;
            float v4 = 0.65f;

            //Determine elements to be removed when left-clicking (the segment/node) in red
            //Determine elements to be removed when right-clicking (the strip/lane) in orange

            var selLane = roadSelection.SelectedLane;
            var selStrip = roadSelection.SelectedLaneStrip;
            var O = new Color(255, 128, 0, 100);
            var R = new Color(255, 0, 0, 100);

            if (selLane != null) {
                //Deleting node/lane

                //Segments
                var node = selLane.RoadNode;
                var fw = node.FrontEnd;
                var bw = node.RearEnd;
                var dependencies = new List<RoadStrip>();
                dependencies.AddRange(fw.ConnectedSegments);
                dependencies.AddRange(bw.ConnectedSegments);
                foreach (var dependency in dependencies) {
                    var segmentTag = dependency.FullSizeTag();
                    if (segmentTag != null) RoadRenderer.GenerateLaneRangeMesh(segmentTag.Value, renderBin, R, v1);
                }

                //Strips
                var fwe = selLane.Front;
                var bwe = selLane.Rear;
                var laneDependencies = new List<LaneStrip>();
                laneDependencies.AddRange(dependencies.SelectMany(x => x.Lanes).Where(x => (x.StartLane.lane == selLane || x.EndLane.lane == selLane)));
                foreach(var laneDependency in laneDependencies) {
                    var laneTag = laneDependency.Tag;
                    RoadRenderer.GenerateLaneRangeMesh(laneTag, renderBin, O, v2);
                }

                //The node/lane itself
                var roadNode = roadSelection.SelectedRoadNode;
                var nodeQuad = RoadRenderer.GenerateRoadNodeSelQuad(roadNode, R, v3);
                renderBin.DrawQuad(nodeQuad);
                var lqp = RoadRenderer.GenerateLaneQuad(selLane, v4, O);
                var q1 = lqp.Back;
                renderBin.DrawQuad(q1);
                var q2 = lqp.Front;
                renderBin.DrawQuad(q2);
            } else if(selStrip != null) {
                //Deleting segment/strip
                var segment = selStrip.road;
                var segmentTag = segment.FullSizeTag();
                if (segmentTag != null) RoadRenderer.GenerateLaneRangeMesh(segmentTag.Value, renderBin, R, v1);
                var stripTag = selStrip.Tag;
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
            RoadSelection MouseOverRoad = game.MouseOverRoad;
            TSWorld world = game.World;

            //Demolish the selected road segment if the left mouse button is clicked
            if (button == MouseButton.Left) {
                // If a road segment is selected, remove it from the world
                var selectedRoad = MouseOverRoad?.SelectedLaneTag?.road;
                var selectedNode = MouseOverRoad?.SelectedRoadNode;
                if (selectedNode != null) {
                    //Demolish a node
                    MouseOverRoad = null;
                    world.Nodes.data.Remove(selectedNode);
                } else if (selectedRoad != null) {
                    log.Trace($"Demolishing road segment: {selectedRoad}");
                    MouseOverRoad = null; // Reset the mouse over road selection
                    world.RoadSegments.data.Remove(selectedRoad); // Remove the selected road segment from the world
                }
            }
            //Demolish the lane on a selected node if the right mouse button is clicked
            if (button == MouseButton.Right) {
                // If a lane tag is selected, remove it from the road segment
                var selectedLaneStrip = MouseOverRoad?.SelectedLaneStrip;
                var selectedNode = MouseOverRoad?.SelectedRoadNode;
                var selectedRoadHalf = MouseOverRoad?.SelectedRoadHalf;
                var selectedLane = MouseOverRoad?.SelectedLane;
                if(selectedLane != null) {
                    //Demolish the node lane
                    MouseOverRoad = null; // Reset the mouse over road selection
                    selectedNode.RemoveLane(selectedLane); // Remove the selected lane from the road node
                }else if(selectedLaneStrip != null) {
                    //Demolish just the lane strip
                    MouseOverRoad = null;
                    selectedLaneStrip.Destroy();
                }
            }

            game.MouseOverRoad = MouseOverRoad;
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.AddAttributes(ISet<string> set) {
            set.Add(ToolAttribs.noHighlights);
        }
    }
}
