using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using TranSimCS.Tools;
using TranSimCS.Tools.Panels;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class SegmentTool : ITool{ 
        public InGameMenu Menu { get; private set; }
        public StripTools StripTools { get; private set; }
        public LaneCreationState? State { get; private set; }
        public SegmentTools SegmentTools { get; private set; }
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
                //Build the node
                RoadNode roadNode = new RoadNode("", State.GeneratedNodePosition);
                var newLaneNodes = State.LaneMappings.EndingLanes;
                var newLanes = new Lane[State.LaneMappings.EndingLanes.Length];
                for (int i = 0; i < newLaneNodes.Length; i++) {
                    var laneDef = newLaneNodes[i];
                    var lane = roadNode.AddLane(laneDef);
                    newLanes[i] = lane;
                }
                Menu.World.Nodes.data.Add(roadNode);

                //Find a new lane to build from
                var nodeSpec = State.StartLane.GetRoadNode().NodeSpec;
                LaneEnd newLaneEnd = default;
                newLaneEnd.end = State.StartLane.end;
                foreach(var laneConnection in State.LaneMappings.Mappings) {
                    if (laneConnection.PassedGuid == State.StartLane.Guid) newLaneEnd.lane = newLanes[laneConnection.EndIndex];
                }

                //If a new lane wasn't found, find a closest one
                if(newLaneEnd.lane == null) {
                    var closestLaneFind = 0;
                    var targetPosition = State.StartLane.lane.MiddlePosition;
                    for (int i = 1; i < State.LaneMappings.EndingLanes.Length; i++) {
                        var candidate = State.LaneMappings.EndingLanes[i];
                        var distance = MathF.Abs(targetPosition - candidate.CenterPos);
                        var closestDistance = MathF.Abs(targetPosition - State.LaneMappings.EndingLanes[closestLaneFind].CenterPos);
                        if (distance < closestDistance) closestLaneFind = i;
                    }
                    newLaneEnd.lane = newLanes[closestLaneFind];
                }
                Debug.Assert(newLaneEnd.lane != null, "Didn't find a new lane");

                //List previous lanes
                var prevLanes = State.StartLane.GetRoadNode().NodeSpec.Select(x => State.StartLane.GetRoadNode().LaneXRef[x.ID]).ToArray();
                
                //Build the road strip
                RoadStrip road = new RoadStrip(State.StartLane.RoadNodeEnd, roadNode.GetEnd(State.StartLane.end.Negate()));
                road.Finish = Menu.configuration.RoadFinish;

                foreach(var connection in State.LaneMappings.Mappings) {
                    var startLane = prevLanes[connection.StartIndex].GetEnd(newLaneEnd.end);
                    var endLane = newLanes[connection.EndIndex].GetEnd(newLaneEnd.end.Negate());
                    var isBackwards = LaneMappings.IsReverseLaneHeuristic(startLane.lane);

                    //backwards if backwards is clearly preferred or equally preferred but going from the back
                    if (isBackwards ^ newLaneEnd.end == NodeEnd.Backward) DataUtil.Swap(ref startLane, ref endLane);
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
            State?.Generate(Menu);
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
            action.Add(ToolAttribs.showSegmentTools);
        }
    }

    public struct LaneMapping {
        public int StartIndex;
        public int EndIndex;
        public LaneSpec LaneSpec;
        public Guid? PassedGuid;
        public LaneMapping(int startIndex, int endIndex, LaneSpec laneSpec, Guid? passedGuid = null) {
            StartIndex = startIndex;
            EndIndex = endIndex;
            LaneSpec = laneSpec;
            PassedGuid = passedGuid;
        }
    }

    public class LaneMappings {
        public int LaneChangesLeft { get; private set; }
        public int LaneChangesRight { get; private set; }
        public ImmutableArray<LaneMapping> Mappings { get; private set; }
        public ImmutableArray<Lane> StartingLanes { get; private set; }
        public ImmutableArray<LaneNode> EndingLanes { get; private set; }
        public Range<float> Range { get; private set; }

        public LaneMappings(LaneCreationState laneCreationState, int laneChangesLeft, int laneChangesRight) {
            //Set fields up
            StartingLanes = laneCreationState.StartLane.GetRoadNode().Lanes.ToImmutableArray();
            LaneChangesLeft = laneChangesLeft;
            LaneChangesRight = laneChangesRight;

            //Find lanes to collapse/expand from
            int leftBound, rightBound = StartingLanes.Length - 1;
            for (leftBound = 0; leftBound <= rightBound; leftBound++) {
                var lane = StartingLanes[leftBound];
                if (lane.Spec.VehicleTypes.HasFlags(VehicleTypes.MotorVehicles)) break;
            }
            for (rightBound = StartingLanes.Length - 1; rightBound >= 0; rightBound--) {
                var lane = StartingLanes[rightBound];
                if (lane.Spec.VehicleTypes.HasFlags(VehicleTypes.MotorVehicles)) break;
            }
            if(leftBound > rightBound) {
                //No car lanes. Map one to one and return
                OneToOne();
                return;
            }

            int trafficLanes = rightBound - leftBound + 1;
            if (trafficLanes + int.Min(laneChangesLeft,0) + int.Min(laneChangesRight,0) <= 0) {
                //Tried to remove too many lanes. Map one to one and return
                OneToOne();
                return;
            }

            /* Lane mapping
             *        |  starting  |
             *        |  core  |
             * ^  |   |   ||   |
             * |  |   .   ||   .\
             * |   \  .   ||   . \
             * |    \ .   ||   .  \
             * |     \.   ||   .   |
             *        |   ||   |   |
             */

            //Count lane mappings
            var numberOfStrips = StartingLanes.Length;
            if (laneChangesLeft > 0) numberOfStrips += laneChangesLeft;
            if (laneChangesRight > 0) numberOfStrips += laneChangesRight;
            var laneMappings = new LaneMapping[numberOfStrips];
            int lmIndex = 0;

            //Count sidewalks
            int countSideLeft = leftBound;
            int countSideRight = StartingLanes.Length - rightBound - 1;

            //Find synthetic lane specs
            var roadSpecLeft = StartingLanes[countSideLeft+1].Spec;
            var roadSpecRight = StartingLanes[^(countSideRight+1)].Spec;
            var sidewalkOffsetLeft = laneChangesLeft * roadSpecLeft.Width;
            var sidewalkOffsetRight = laneChangesRight * roadSpecRight.Width;

            //Copy sidewalks
            int newCount = StartingLanes.Length + laneChangesLeft + laneChangesRight;
            var endingLanes = new LaneNode[newCount];
            for (int i = 0; i < countSideLeft; i++) {
                var existingSidewalk = StartingLanes[i];
                var newSidewalk = new LaneNode(existingSidewalk.Spec, existingSidewalk.MiddlePosition - sidewalkOffsetLeft);
                endingLanes[i] = newSidewalk;
                var lm = new LaneMapping(i, i, existingSidewalk.Spec, existingSidewalk.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }
            for(int i = 0; i < countSideRight; i++) {
                var existingSidewalk = StartingLanes[^i];
                var newSidewalk = new LaneNode(existingSidewalk.Spec, existingSidewalk.MiddlePosition + sidewalkOffsetRight);
                endingLanes[^i] = newSidewalk;
                var lm = new LaneMapping(StartingLanes.Length - i - 1, newCount - i - 1, existingSidewalk.Spec, existingSidewalk.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }

            //Constants
            var mergeFlagsMask = LaneFlags.MergeRight | LaneFlags.MergeLeft | LaneFlags.IsMerge;
            var mergePlain = LaneFlags.IsMerge;
            var mergeLeft = LaneFlags.IsMerge | LaneFlags.MergeLeft;
            var mergeRight = LaneFlags.IsMerge | LaneFlags.MergeRight;
            var exitLeft = LaneFlags.MergeLeft;
            var exitRight = LaneFlags.MergeRight;

            //Count core lanes
            int coreOffsetStart = countSideLeft;
            if (laneChangesLeft < 0) coreOffsetStart -= laneChangesLeft;
            int coreOffsetEnd = countSideRight;
            if (laneChangesLeft > 0) coreOffsetEnd += laneChangesLeft;
            int coreCount = 1 + rightBound - leftBound;
            if (laneChangesLeft < 0) coreCount += laneChangesLeft;
            if(laneChangesRight < 0) coreCount += laneChangesRight;

            //Make left/right expanded/merged lanes
            if (laneChangesLeft > 0) {
                //Expansion on the left
                for(int i = 0; i < laneChangesLeft; i++) {
                    int newIndex = i + leftBound;
                    int prevIndex = leftBound;
                    var spec = roadSpecLeft;
                    var newflags = (i == 0) ? exitLeft : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                    var lane = StartingLanes[prevIndex];
                    var lanenode = new LaneNode(lane.Spec, lane.MiddlePosition - (i + 1) * lane.Spec.Width);
                    endingLanes[newIndex] = lanenode;
                }
            } else {
                //Merge on the left
                for (int i = 0; i < -laneChangesLeft; i++) {
                    int newIndex = leftBound;
                    int prevIndex = i + leftBound;
                    var spec = roadSpecLeft;
                    var newflags = (i == 0) ? mergeLeft : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                }
            }
            if (laneChangesRight > 0) {
                //Expansion on the right
                for (int i = 0; i < laneChangesRight; i++) {
                    int newIndex = newCount - countSideRight - i - 1;
                    int prevIndex = rightBound;
                    var spec = roadSpecRight;
                    var newflags = (i == 0) ? exitRight : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                    var lane = StartingLanes[prevIndex];
                    var lanenode = new LaneNode(lane.Spec, lane.MiddlePosition + (i + 1) * lane.Spec.Width);
                    endingLanes[newIndex] = lanenode;
                }
            } else {
                //Merge on the right
                for (int i = 0; i < -laneChangesRight; i++) {
                    int newIndex = rightBound + laneChangesRight + laneChangesLeft;
                    int prevIndex = StartingLanes.Length - countSideRight - i - 1;
                    var spec = roadSpecRight;
                    var newflags = (i == 0) ? mergeRight : mergePlain;
                    spec.Flags = (spec.Flags & ~mergeFlagsMask) | newflags;
                    var lm = new LaneMapping(prevIndex, newIndex, spec);
                    ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                    laneMappings[lmIndex++] = lm;
                }
            }

            //Copy core lanes
            for (int i = 0; i < coreCount; i++) {
                int startIndex = coreOffsetStart + i;
                int endIndex = coreOffsetEnd + i;
                var startLane = StartingLanes[startIndex];
                var endLane = new LaneNode(startLane.Spec, startLane.MiddlePosition);
                endingLanes[endIndex] = endLane;
                var lm = new LaneMapping(startIndex, endIndex, startLane.Spec, startLane.Guid);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                laneMappings[lmIndex++] = lm;
            }

            //Calculate end bounds
            Range = new();
            foreach(var endLane in endingLanes) {
                Range = Range.Union(endLane.Bounds);
            }

            //Push results
            Debug.Assert(lmIndex == laneMappings.Length, "Not all lane mappings are populated");
            Debug.Assert(newCount > 0, "Returned with no output lanes");
            EndingLanes = endingLanes.ToImmutableArray();
            Mappings = laneMappings.ToImmutableArray();
        }
        [Conditional("DEBUG")]
        private void ValidateMappings(int startCount, int endCount, LaneMapping mapping) {
            Debug.Assert(mapping.StartIndex >= 0 && mapping.StartIndex < startCount, "Out of bounds start index");
            Debug.Assert(mapping.EndIndex >= 0 && mapping.EndIndex < endCount, "Out of bounds end index");
        }
        private void OneToOne() {
            int count = StartingLanes.Length;
            var endingLanes = new LaneNode[count];
            var mappings = new LaneMapping[count];
            for (int i = 0; i < count; i++) {
                var laneDef = StartingLanes[i];
                var newLaneDef = new LaneNode(laneDef.Spec, laneDef.MiddlePosition, Guid.NewGuid());
                var isReverse = IsReverseLaneHeuristic(laneDef);
                endingLanes[i] = newLaneDef;
                var lm = new LaneMapping(i, i, laneDef.Spec);
                ValidateMappings(StartingLanes.Length, endingLanes.Length, lm);
                mappings[i] = lm;
            }
            Mappings = mappings.ToImmutableArray();
            EndingLanes = endingLanes.ToImmutableArray();
            Range = new();
            foreach (var endLane in endingLanes) {
                Range = Range.Union(endLane.Bounds);
            }
        }
        public static bool IsReverseLaneHeuristic(Lane lane) {
            //Should the road go forward or backward?
            var (forwardCount, backwardCount) = lane.CountLaneDirections();

            var isBackPreferred = backwardCount > forwardCount;
            var isForwardPreferred = backwardCount < forwardCount;
            var isLaneLeft = lane.MiddlePosition < 0;

            var isBackwards = isBackPreferred || (!isForwardPreferred && isLaneLeft);
            return isBackwards;
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
        public LaneMappings LaneMappings;

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
            var segmentTools = menu.ToolsPanel.GetPanel<SegmentTools>(ToolAttribs.showSegmentTools);
            var laneChangesLeft = segmentTools.AddRemoveLeft.Value;
            var laneChangesRight = segmentTools.AddRemoveRight.Value;
            if (LaneMappings == null || LaneMappings.LaneChangesLeft != laneChangesLeft || LaneMappings.LaneChangesRight != laneChangesRight)
                LaneMappings = new LaneMappings(this, laneChangesLeft, laneChangesRight);

            var stripTools = menu.ToolsPanel.GetPanel<StripTools>(ToolAttribs.showRoadTools);
            SplineMode = menu.RoadCreationTool.Mode;
            Alignment = stripTools.AlignmentProp.Value;
            StartRange = StartLane.GetRoadNode().Bounds;
            EndRange = LaneMappings.Range;

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
