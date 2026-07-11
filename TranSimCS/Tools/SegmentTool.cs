using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.ClassInstances.Pred;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Tools.Panels;

namespace TranSimCS.Tools {
    public class SegmentTool : ITool{ 
        public InGameMenu Menu { get; private set; }
        public StripTools StripTools { get; private set; }
        public SegmentTools SegmentTools { get; private set; }

        //TOOL STATE
        public LaneCreationState? State { get; private set; }
        public LaneMappings? LaneMappings { get; private set; }

        //PROPERTIES
        public string Name => "Road Creation Tool 2";
        public string Description => (State == null) ?
            "Pick a lane to start creating a road segment" :
            State.GenerateDescription();
            

        public SegmentTool(InGameMenu menu) {
            Menu = menu;
            StripTools = menu.ToolsPanel.GetPanel<StripTools>(ToolAttribs.showRoadTools);
            SegmentTools = menu.ToolsPanel.GetPanel<SegmentTools>(ToolAttribs.showSegmentTools);
        }

        public (object[], string)[] PromptKeys() {
            if (State == null) return [
                ([MouseButton.Left], "Select a road node end to create a lane strip."),

                ([Keys.Q], "to add a lane on the left"),
                ([Keys.E], "to subtract a lane on the left"),
                ([Keys.O], "to subtract a lane on the right"),
                ([Keys.P], "to add a lane on the right"),
                ([Keys.Z], "to move starting left border left"),
                ([Keys.C], "to move starting left border right"),
                ([Keys.OemComma], "to move starting right border left"),
                ([Keys.OemPeriod], "to move starting right border right"),
                ([Keys.X], "to swap left and right include/exclude counts"),
                ([Keys.OemQuestion], "to toggle include/exclude"),
            ]; return [
                ([MouseButton.Right], "to cancel"),
                ([MouseButton.Left], "to place a point. Changes will be reset afterwards."),

                ([Keys.Q], "to add a lane on the left"),
                ([Keys.E], "to subtract a lane on the left"),
                ([Keys.O], "to subtract a lane on the right"),
                ([Keys.P], "to add a lane on the right"),
                ([Keys.Z], "to move starting left border left"),
                ([Keys.C], "to move starting left border right"),
                ([Keys.OemComma], "to move starting right border left"),
                ([Keys.OemPeriod], "to move starting right border right"),
                ([Keys.OemQuestion], "to swap left and right include/exclude counts"),
                ([Keys.X], "to toggle include/exclude"),
            ];
        }

        void ITool.OnKeyDown(Keys key) {
            var changeLaneCountPolarity = (SegmentTools.IsInclusive.Value ? 1 : -1);
            switch (key) {
                case Keys.Q:
                    SegmentTools.AddRemoveLeft.Value += 1;
                    break;
                case Keys.E:
                    SegmentTools.AddRemoveLeft.Value -= 1;
                    break;
                case Keys.O:
                    SegmentTools.AddRemoveRight.Value -= 1;
                    break;
                case Keys.P:
                    SegmentTools.AddRemoveRight.Value += 1;
                    break;
                case Keys.Z:
                    SegmentTools.IncludeExcludeLeft.Value += (uint)changeLaneCountPolarity;
                    break;
                case Keys.C:
                    SegmentTools.IncludeExcludeLeft.Value -= (uint)changeLaneCountPolarity;
                    break;
                case Keys.OemComma:
                    SegmentTools.IncludeExcludeRight.Value -= (uint)changeLaneCountPolarity;
                    break;
                case Keys.OemPeriod:
                    SegmentTools.IncludeExcludeRight.Value += (uint)changeLaneCountPolarity;
                    break;
                case Keys.X:
                    SegmentTools.IsInclusive.Value = !SegmentTools.IsInclusive.Value;
                    break;
                case Keys.OemQuestion:
                    var l = SegmentTools.IncludeExcludeRight.Value;
                    var r = SegmentTools.IncludeExcludeLeft.Value;
                    SegmentTools.IncludeExcludeRight.Value = r;
                    SegmentTools.IncludeExcludeLeft.Value = l;
                    break;
            }
        }

        void ITool.OnClick(MouseButton button) {
            var pickedGroundPosition = Menu.GroundSelection;
            if(State == null && button == MouseButton.Left){
                //Pick a new selection
                var pickedLaneEnd = Menu.MouseOver?.As<LaneEnd>();
                if (pickedLaneEnd == null || pickedLaneEnd.Value.lane == null) return;
                State = new LaneCreationState(pickedLaneEnd.Value);
            }else if(State != null && button == MouseButton.Left) {
                //Validate the position
                if (!float.IsFinite(State.GeneratedNodePosition.Inclination) || !float.IsFinite(State.GeneratedNodePosition.Tilt)) return;

                //Build the node
                RoadNode roadNode = new RoadNode("", State.GeneratedNodePosition);
                var newLaneNodes = LaneMappings!.EndingLanes;
                var newLanes = new Lane[LaneMappings.EndingLanes.Length];
                for (int i = 0; i < newLaneNodes.Length; i++) {
                    var laneDef = newLaneNodes[i];
                    var lane = roadNode.AddLane(laneDef);
                    newLanes[i] = lane;
                }
                Menu.World.Nodes.data.Add(roadNode);

                //Find a new lane to build from
                var passthroughNumber = LaneMappings.LaneMappingSourceToDest;
                LaneEnd newLaneEnd = new LaneEnd(State.StartLane.end, newLanes[passthroughNumber]);
                Debug.Assert(newLaneEnd.lane != null, "Didn't find a new lane");

                //List previous lanes
                var prevLanes = LaneMappings.StartingLanes;
                
                //Build the road strip
                RoadStrip road = new RoadStrip(State.StartLane.RoadNodeEnd, roadNode.GetEnd(State.StartLane.end.Negate()));
                road.Finish = Menu.configuration.RoadFinish;
                foreach(var connection in LaneMappings.Mappings) {
                    var startLane = prevLanes[connection.StartIndex].GetEnd(newLaneEnd.end);
                    var endLane = newLanes[connection.EndIndex].GetEnd(newLaneEnd.end.Negate());
                    var isBackwards = LaneMappings.IsReverseLaneHeuristic(startLane.lane);

                    //backwards if backwards is clearly preferred or equally preferred but going from the back
                    if (isBackwards ^ newLaneEnd.end == NodeEnd.Backward) DataUtil.Swap(ref startLane, ref endLane);
                    LaneStrip laneStrip = new LaneStrip(startLane, endLane);
                    var spec = connection.LaneSpec;
                    if (isBackwards) spec.Flags = spec.Flags.LongitudinalReverse();
                    if (newLaneEnd.end == NodeEnd.Backward) spec.Flags = spec.Flags.Reverse();
                    laneStrip.Spec = spec;
                    road.AddLaneStrip(laneStrip);
                }
                Menu.World.RoadSegments.data.Add(road);

                //Advance to the next road node
                State = new LaneCreationState(newLaneEnd);
                SegmentTools.AddRemoveLeft.Value = 0;
                SegmentTools.AddRemoveRight.Value = 0;
                SegmentTools.IncludeExcludeLeft.Value = 0;
                SegmentTools.IncludeExcludeRight.Value = 0;
                SegmentTools.IsInclusive.Value = false;
                LaneMappings = null;
            } else if (State != null && button == MouseButton.Right) {
                //Quit road creation
                State = null;
                LaneMappings = null;
            }
        }
        void ITool.Update(GameTime gameTime) {
            var presets = SegmentTools.CurrentPresets;
            if (State != null && LaneMappings?.Presets != presets)
                LaneMappings = new LaneMappings(State, presets);

            State?.StartRange = LaneMappings!.StartRange;
            State?.EndRange = LaneMappings!.EndRange;
            State?.Generate(Menu);
            
        }
        void ITool.Draw(GameTime gameTime) {
            if (State == null || !float.IsFinite(State.GeneratedNodePosition.Inclination) || !float.IsFinite(State.GeneratedNodePosition.Tilt)) return;
            Color previewColor = Colors.SemiClearWhite;
            var material = Assets.Asphalt;
            material.BlendMode = ModelOld.MaterialBlendMode.Transparent;

            var accuracy = Settings.RoadAccuracy;
            var apshaltBin = Menu.renderHelper.GetOrCreateRenderBinForced(material);
            var leftPoints = GeometryUtils.GenerateSplinePoints(State.GeneratedSplines.left, accuracy);
            var rightPoints = GeometryUtils.GenerateSplinePoints(State.GeneratedSplines.right, accuracy);
            var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftPoints, rightPoints, StripRenderer.GenerateLaneStripVertexGen(previewColor));
            apshaltBin.DrawStrip(generatedVertStripPair);
        }


        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showFinishes);
            action.Add(ToolAttribs.showRoadTools);
            action.Add(ToolAttribs.showSnapOptions);
            action.Add(ToolAttribs.showSegmentTools);
        }
    }
}
