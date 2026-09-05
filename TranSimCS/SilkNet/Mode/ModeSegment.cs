using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Tools.RoadConstruction;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet.Mode {
    public class ModeSegment : IMode{ 
        public SilkNetTest Menu { get; private set; }

        public StripTools StripTools { get; private set; }

        //TOOL STATE
        public LaneCreationState? State { get; private set; }
        public LaneMappings? LaneMappings { get; private set; }

        //PROPERTIES
        string IMode.Title() => "Road Creation Tool 2";
        void IMode.DrawUI() {
            if (ImGui.Begin("Road segment tools")) {
                ImGui.Text("[Q] to add a lane on the left");
                ImGui.Text("[E] to merge a lane on the left");
                ImGui.Text("[O] to merge a lane on the right");
                ImGui.Text("[P] to add a lane on the right");
                if (Menu.SegmentPresets.IsInclusive) {
                    ImGui.Text("Current mode: inclusive");
                    ImGui.Text("[Z] to include a lane on the left");
                    ImGui.Text("[C] to exclude a lane on the left");
                    ImGui.Text("[,] to exclude a lane on the right");
                    ImGui.Text("[.] to include a lane on the right");
                    ImGui.Text("[/] to switch to exclusive mode (counts are number of lanes to exclude from the node)");
                    ImGui.Text("[X] to swap left/right include counts");
                } else {
                    ImGui.Text("Current mode: exclusive");
                    ImGui.Text("[Z] to exclude a lane on the left");
                    ImGui.Text("[C] to include a lane on the left");
                    ImGui.Text("[,] to include a lane on the right");
                    ImGui.Text("[.] to exclide a lane on the right");
                    ImGui.Text("[/] to switch to inclusive mode (counts are number of lanes to include on either side of start lane)");
                    ImGui.Text("[X] to swap left/right include counts");
                }
                ImGui.Text("[LAlt] to cycle direction modes");
                ImGui.Text("[PageUp] to increase the height");
                ImGui.Text("[PageDown] to decrease the height");


                if (State == null) {
                    ImGui.Text("[LMB] Select a half-lane to create a lane strip");
                } else {
                    ImGui.Text("[LMB] to place the segment. Changes will be reset afterwards.");
                    ImGui.Text("[RMB] to cancel");
                }
                
                Menu.ShowSnappingSettings();
                Menu.ShowSegmentPresets();
                Menu.ShowFinishSettings();
                Menu.ShowLaneCreator();

                ImGui.End();
            }
            
        }
            

        public ModeSegment(SilkNetTest menu) {
            Menu = menu;
        }

        void IMode.OnKeyPress(Key key) {
            var changeLaneCountPolarity = Menu.SegmentPresets.IsInclusive ? 1 : -1;
            switch (key) {
                case Key.Q:
                    Menu.SegmentPresets.AddRemoveLeft++;
                    break;
                case Key.E:
                    Menu.SegmentPresets.AddRemoveLeft--;
                    break;
                case Key.O:
                    Menu.SegmentPresets.AddRemoveRight--;
                    break;
                case Key.P:
                    Menu.SegmentPresets.AddRemoveRight++;
                    break;
                case Key.Z:
                    Menu.SegmentPresets.IncludeExcludeLeft += (uint)changeLaneCountPolarity;
                    break;
                case Key.C:
                    Menu.SegmentPresets.IncludeExcludeLeft -= (uint)changeLaneCountPolarity;
                    break;
                case Key.Comma:
                    Menu.SegmentPresets.IncludeExcludeRight -= (uint)changeLaneCountPolarity;
                    break;
                case Key.Period:
                    Menu.SegmentPresets.IncludeExcludeRight += (uint)changeLaneCountPolarity;
                    break;
                case Key.X:
                    Menu.SegmentPresets.IsInclusive ^= true;
                    break;
                case Key.Slash:
                    DataUtil.Swap(
                        ref Menu.SegmentPresets.IncludeExcludeLeft,
                        ref Menu.SegmentPresets.IncludeExcludeRight
                    );
                    break;
                case Key.AltLeft:
                    Menu.SegmentPresets.DirectionChoice = Menu.SegmentPresets.DirectionChoice.Next();
                    break;
                case Key.PageUp:
                    StripTools.Height.Value += StripTools.HeightStep.Value;
                    break;
                case Key.PageDown:
                    StripTools.Height.Value -= StripTools.HeightStep.Value;
                    break;
            }
        }

        void IMode.OnMousePress(Silk.NET.Input.MouseButton button) {
            if (State == null && button == MouseButton.Left) {
                //Pick a new selection
                var picked = Menu.MouseOver?.As<IRoadElement>();
                if (picked is AddLaneSelection als) {
                    var nodeEnd = als.nodeEnd;
                    var insertPos = als.CalculateOffset(Menu.LaneSpec.Width/2);
                    picked = nodeEnd.Node.AddLane(new(Menu.LaneSpec, insertPos)).GetHalfLane(nodeEnd.End);
                }
                if (picked is HalfLane pickedLaneEnd && pickedLaneEnd.Lane != null) {
                    //Start from an existing lane
                    State = new LaneCreationState(pickedLaneEnd);
                }

            } else if (State != null && button == MouseButton.Left) {
                //Validate the position
                if (LaneMappings == null || !State.GeneratedNodePosition.IsFinite()) return;

                //Advance to the next road node
                State = LaneReconcillation.BuildConnections(State, LaneMappings, Menu);
                Menu.SegmentPresets.AddRemoveLeft = 0;
                Menu.SegmentPresets.AddRemoveRight = 0;
                Menu.SegmentPresets.IncludeExcludeLeft = 0;
                Menu.SegmentPresets.IncludeExcludeRight = 0;
                Menu.SegmentPresets.IsInclusive = false;
                LaneMappings = null;
            } else if (State != null && button == MouseButton.Right) {
                //Quit road creation
                State = null;
                LaneMappings = null;
            }
        }
        void IMode.Update(double dt) {
            //Check if to flip the state
            var mousePosition = GeometryUtils.IntersectRayPlane(Menu.MouseRay, Menu.snappingGrid.CreateSnappingPlane());
            if (State != null && mousePosition.IsFinite()) {
                var nodeTangent = State.StartLane.HalfNode.Cache.ReferenceFrame.Z;
                var toMouseVector = mousePosition - State.StartLane.HalfNode.CenterPos;
                if (Vector3.Dot(nodeTangent, toMouseVector) < -0.0001)
                    //On the back of the state. Invert
                    State = new LaneCreationState(State.StartLane.OppositeHalf);
            }

            var presets = Menu.SegmentPresets;
            if (State != null && (LaneMappings?.Presets != presets || LaneMappings?.LaneCreationState != State))
                LaneMappings = new LaneMappings(State, presets);

            State?.StartRange = LaneMappings!.StartRange;
            State?.EndRange = LaneMappings!.EndRange;
            State?.Generate(Menu);
        }

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            if (State == null || !float.IsFinite(State.GeneratedNodePosition.Inclination) || !float.IsFinite(State.GeneratedNodePosition.Tilt)) return;
            Color previewColor = new Color(64, 64, 64, 128);
            var material = Assets.Asphalt;
            material.BlendMode = ModelOld.MaterialBlendMode.Transparent;

            var accuracy = Settings.RoadAccuracy;
            var apshaltBin = renderMeshPool.GetOrCreateRenderBinForced(material);
            var leftPoints = GeometryUtils.GenerateSplinePoints(State.GeneratedSplines.left, accuracy);
            var rightPoints = GeometryUtils.GenerateSplinePoints(State.GeneratedSplines.right, accuracy);
            var chordLengthL = Vector3.DistanceSquared(leftPoints[0], leftPoints[^1]);
            var chordLengthR = Vector3.DistanceSquared(rightPoints[0], rightPoints[^1]);
            if (chordLengthL < 0.01 && chordLengthR < 0.01) return;
            var generatedVertStripPair = UniformTexturing.UniformTexturedTwin(leftPoints, rightPoints, UniformTexturing.GenerateLaneStripVertexGen(previewColor));
            apshaltBin.DrawStrip(generatedVertStripPair);

            //Generate a preview of the node position
            var refframe = State.GeneratedNodePosition.CalcReferenceFrame();
            var roadRenderBin = renderMeshPool.GetOrCreateRenderBinForced(Assets.Road);
            var front = refframe.O + refframe.Z * 2;
            var back = refframe.O - refframe.Z * 2;
            roadRenderBin.DrawLine(refframe.O, front, refframe.Y, Colors.Red);
            roadRenderBin.DrawLine(refframe.O, back, refframe.Y, Colors.Maroon);

            //Generate endpoint previews
            if (State == null || LaneMappings == null) return;
            var centerSpline = State.GeneratedSplines.Middle;
            var splineEndTangent = Vector3.Normalize(centerSpline.d - centerSpline.c);
            if (!splineEndTangent.IsFinite()) return;
            var arrowBin = renderMeshPool.GetOrCreateRenderBinForced(Assets.Arrow);
            foreach (var laneNode in LaneMappings.EndingLanes) {
                var laneCenter = laneNode.CenterPos;
                var laneWidth = laneNode.LaneSpec.Width;
                var laneLength = laneWidth * 2;

                var startingPos = refframe.O + refframe.X * laneCenter * State.DestinationNodeEnd.Discriminant();
                var endingPos = startingPos + splineEndTangent * laneLength;
                arrowBin.DrawLine(startingPos, endingPos, refframe.Y, laneNode.LaneSpec.Color, laneWidth);
            }
        }
        void IMode.AddSelectors(MultiMesh invisible, MultiMesh visible) {
            SelectionUtils.AddAddLaneSelectors(visible, Menu);
        }
    }
}
