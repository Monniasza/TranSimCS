using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FontStashSharp;
using LanguageExt;
using LanguageExt.ClassInstances.Pred;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using MonoGame.Extended;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class SegmentTool : ITool{ 
        public InGameMenu Menu { get; private set; }
        public StripTools StripTools { get; private set; }
        public LaneCreationState? State { get; private set; }
        public string Name => "Road Creation Tool 2";
        public string Description => (State == null) ?
            "Pick a lane to start creating a road segment" :
            State.GenerateDescription();
            

        public SegmentTool(InGameMenu menu) {
            Menu = menu;
            StripTools = menu.ToolsPanel.GetPanel<StripTools>(ToolAttribs.showRoadTools);
        }

        public (object[], string)[] PromptKeys() {
            if (State == null) return [
                ([MouseButton.Left], "Select a road node end to create a lane strip.")
            ]; return [
                ([MouseButton.Right], "to cancel"),
                ([MouseButton.Left], "to place a point. Changes will be reset afterwards.")
            ];
        }

        void ITool.OnClick(MouseButton button) {
            var pickedGroundPosition = Menu.GroundSelection;
            if(State == null && button == MouseButton.Left){
                //Pick a new selection
                var pickedLaneEnd = Menu.MouseOver?.As<LaneEnd>();
                if (pickedLaneEnd == null || pickedLaneEnd.Value.lane == null) return;
                State = new LaneCreationState(pickedLaneEnd.Value);
            }else if(State != null && button == MouseButton.Left) {
                //Place a node

                //Build the node
                LaneEnd newLaneEnd = default;
                newLaneEnd.end = State.StartLane.end;
                RoadNode roadNode = new RoadNode("", State.GeneratedNodePosition);
                var nodeSpec = State.StartLane.GetRoadNode().NodeSpec;
                var prevLanes = new Lane[nodeSpec.Lanes.Count];
                var newLanes = new Lane[nodeSpec.Lanes.Count];
                for (int i = 0; i < nodeSpec.Lanes.Count; i++) {
                    var laneDef = nodeSpec.Lanes[i];
                    var newLaneDef = new LaneNode(laneDef.LaneSpec, laneDef.CenterPos, Guid.NewGuid());
                    var lane = roadNode.AddLane(newLaneDef);
                    newLanes[i] = lane;
                    prevLanes[i] = State.StartLane.GetRoadNode().LaneXRef[laneDef.ID];
                    if (laneDef.ID == State.StartLane.Guid) newLaneEnd.lane = lane;
                }
                Debug.Assert(newLaneEnd.lane != null, "Didn't find a new lane");
                Menu.World.Nodes.data.Add(roadNode);

                //Build the road strip
                RoadStrip road = new RoadStrip(State.StartLane.RoadNodeEnd, roadNode.GetEnd(State.StartLane.end.Negate()));
                for(int i = 0; i < nodeSpec.Lanes.Count; i++) {
                    var startLane = prevLanes[i].GetEnd(newLaneEnd.end);
                    var endLane = newLanes[i].GetEnd(newLaneEnd.end.Negate());

                    //Should the road go forward or backward?
                    var (forwardCount, backwardCount) = startLane.lane.CountLaneDirections();
                    //backwards if backwards is clearly preferred or equally preferred but going from the back
                    if ((backwardCount > forwardCount ^ newLaneEnd.end == NodeEnd.Backward) && backwardCount != forwardCount) DataUtil.Swap(ref startLane, ref endLane);
                    LaneStrip laneStrip = new LaneStrip(startLane, endLane);
                    road.AddLaneStrip(laneStrip);
                }
                Menu.World.RoadSegments.data.Add(road);

                //Advance to the next road node
                State = new LaneCreationState(newLaneEnd);
            } else if (State != null && button == MouseButton.Right) {
                //Quit road creation
                State = null;
            }
        }
        void ITool.Update(GameTime gameTime) {
            if(State != null) {
                var bounds = State.StartLane.GetRoadNode().Bounds;
                State.StartRange = State.EndRange = bounds;
                State.Generate(Menu);
            }
            
        }
        void ITool.Draw(GameTime gameTime) {
            if (State == null) return;
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
        }
    }

    public class LaneCreationState {
        //SOURCE STATE
        public readonly LaneEnd StartLane;
        public Range<float> StartRange;
        public Range<float> EndRange;

        //EXTERNAL SOURCE STATE
        public Vector3 TargetPosition;
        public RoadMode SplineMode;
        public Alignment Alignment;

        //GENERATED STATE
        public Strip GeneratedSplines;
        public PositionEulerAngles GeneratedNodePosition;
        public RoadPlan RoadPlan;

        //DERIVED STATE
        public Bezier3 CenterLine => GeneratedSplines.Middle;

        //CONSTRUCTOR
        public LaneCreationState(LaneEnd laneEnd) {
            ArgumentNullException.ThrowIfNull(laneEnd.lane, nameof(laneEnd.lane));
            StartLane = laneEnd;
        }

        //REPRESENTATION
        public String GenerateDescription() =>
            $"Creating a segment. Chord-length: {CenterLine.ChordLength()}, arc-length: {CenterLine.ArcLength()}";
        
        //GENERATION
        public void Generate(InGameMenu menu) {
            var stripTools = menu.ToolsPanel.GetPanel<StripTools>(ToolAttribs.showRoadTools);
            SplineMode = menu.RoadCreationTool.Mode;
            Alignment = stripTools.AlignmentProp.Value;

            var (alignmentMulLeft, alignmentMulRight) = Alignment.GetAlignments();
            var laneRange = StartLane.lane.Bounds;
            var referenceIndex = alignmentMulLeft * laneRange.Min + alignmentMulRight * laneRange.Max;
            var startingPositionRef = LineEnd.calcLineEnd(StartLane.RoadNodeEnd, referenceIndex);
            var startTangent = startingPositionRef.Tangential;
            var startLateral = startingPositionRef.Lateral;
            startLateral *= StartLane.end.Discriminant();
            var startPos = startingPositionRef.Position;
            var startWidth = StartLane.lane.Width;
            Plane selectionPlane = menu.ReferencePlane;
            TargetPosition = GeometryUtils.IntersectRayPlane(menu.MouseRay, selectionPlane);
            if (menu.CheckSnap.Checked) 
                //Snap the position
                TargetPosition = menu.configuration.SnapGrid.Snap(TargetPosition);
            RoadPlan plan = new RoadPlan {
                startLateral = startLateral,
                endLateral = startLateral,
                startPos = startPos,
                endPos = TargetPosition,
                startTangent = startTangent,
                endTangent = startTangent,
                menu = menu
            };
            plan.Align(Alignment, startWidth);
            //Apply the road mode
            SplineMode.CreateValues(plan);
            plan.Align(Alignment.Inverse(), startWidth);
            RoadPlan = plan;

            //Flatten tilt or inclination
            if (stripTools.flattenTilt.Checked) plan.endLateral = plan.endLateral.ToX0Z().Normalized();
            if (stripTools.flattenIncline.Checked) plan.endTangent = plan.endTangent.ToX0Z().Normalized();
            var endLeftPos = plan.endPos - plan.endLateral * StartLane.lane.Width / 2;

            var correctedLateral = plan.endLateral * StartLane.end.Discriminant();
            var correctedPosition = plan.endPos - correctedLateral * StartLane.lane.LeftPosition;

            //Calculate the NodePosition
            var newNodePosition = PositionEulerAngles.FromPosTangentLateral(correctedPosition, plan.endTangent, correctedLateral);
            if (StartLane.end == NodeEnd.Backward) newNodePosition.Azimuth += int.MinValue;
                GeneratedNodePosition = newNodePosition;
            if (stripTools.flattenTilt.Checked) newNodePosition.Tilt = 0;
            if (stripTools.flattenIncline.Checked) newNodePosition.Inclination = 0;

            Bezier3 lbound = GeometryUtils.GenerateJoinSpline(startPos + plan.startLateral * (StartRange.Min - referenceIndex), plan.endPos + plan.endLateral * (EndRange.Min - referenceIndex), startTangent, -plan.endTangent);
            Bezier3 rbound = GeometryUtils.GenerateJoinSpline(startPos + plan.startLateral * (StartRange.Max - referenceIndex), plan.endPos + plan.endLateral * (EndRange.Max - referenceIndex), startTangent, -plan.endTangent);
            if (StartLane.end == NodeEnd.Backward) DataUtil.Swap(ref lbound, ref rbound);
            GeneratedSplines = new(lbound, rbound);
        }
    
    }
}
