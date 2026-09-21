using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Mode.RoadConstruction;
using TranSimCS.SilkNet.RoadConstruction;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.Setting;
using TranSimCS.SilkNet;
using TranSimCS.Worlds;

namespace TranSimCS.Mode {
    public enum RoadBuilderPhase {
        PickStart, PickEnd, Edit
    }

    /// <summary>
    /// One end of the segment being built by the <see cref="ModeRoadBuilder"/>.
    /// Either attached to an existing node end (then the spec is a read-only copy), or floating (fully editable).
    /// </summary>
    public class RoadBuilderSide {
        public RoadNodeEnd? Picked;
        public PositionEulerAngles FloatingPosition;
        public readonly List<LaneNode> Lanes = new();

        public bool IsFloating => Picked == null;

        /// <summary>Initializes the lane list from the picked node end, ordered left to right.</summary>
        public void CaptureFromPicked() {
            Lanes.Clear();
            if (Picked?.HalfNode == null) return;
            foreach (var halfLane in Picked.HalfNode.GetLaneList().OrderBy(x => x.MiddlePosition))
                Lanes.Add(halfLane.LaneNode);
        }
    }

    /// <summary>
    /// A copyable definition of a whole road segment: specs of both of its ends.
    /// </summary>
    public readonly record struct RoadBuilderTemplate(NodeSpec StartSpec, NodeSpec EndSpec);

    /// <summary>
    /// Clipboard for <see cref="NodeSpec"/>s and <see cref="RoadBuilderTemplate"/>s.
    /// Static, so copied specs survive tool switches.
    /// </summary>
    public static class RoadBuilderClipboard {
        public static NodeSpec? CopiedSpec;
        public static RoadBuilderTemplate? CopiedTemplate;

        public static List<LaneNode> Reguid(NodeSpec spec) =>
            spec.Select(lane => new LaneNode(lane.LaneSpec, lane.CenterPos)).ToList();
    }

    /// <summary>
    /// Road Builder - builds a road segment between two arbitrary ends, each either attached
    /// to an existing node end or floating. Both end specs are directly editable lane lists
    /// (any vehicle type combination, merges and expands at any position), and can be copied,
    /// edited and pasted, together with whole segment templates.
    /// Inspired by the Road Builder mod for Cities: Skylines.
    /// </summary>
    public class ModeRoadBuilder : IMode {
        public SilkNetTest Menu { get; private set; }

        //TOOL STATE
        public RoadBuilderPhase Phase;
        public RoadBuilderSide Start = new();
        public RoadBuilderSide End = new();
        public int ActiveSide;
        public int SelectedStartLane = -1;
        public int SelectedEndLane = -1;
        public string Message = "";

        public ModeRoadBuilder(SilkNetTest menu) {
            Menu = menu;
        }

        string IMode.Title() => "Road Builder";

        private RoadBuilderSide Active => ActiveSide == 0 ? Start : End;

        void IMode.DrawUI() {
            if (!ImGui.Begin("Road Builder")) {
                ImGui.End();
                return;
            }
            ImGui.Text(Phase switch {
                RoadBuilderPhase.PickStart => "[LMB] pick a node end to start from, or empty space for a floating start",
                RoadBuilderPhase.PickEnd => "[LMB] pick a node end to finish at, or empty space for a floating end",
                _ => "[Enter] to build the segment. [RMB] to reset."
            });
            if (Message.Length > 0) ImGui.Text(Message);

            Menu.ShowSnappingSettings();
            Menu.ShowFinishSettings();

            if (Phase != RoadBuilderPhase.Edit) {
                if (ImGui.Button("Reset [RMB]")) Reset();
                ImGui.End();
                return;
            }

            //Active side toggle
            if (ImGui.Button(ActiveSide == 0 ? "> Start <" : "Start")) ActiveSide = 0;
            ImGui.SameLine();
            if (ImGui.Button(ActiveSide == 1 ? "> End <" : "End")) ActiveSide = 1;

            ShowSide("Start", Start, ref SelectedStartLane);
            ShowSide("End", End, ref SelectedEndLane);

            ShowMappingPreview();

            //Whole segment templates
            ImGui.Separator();
            if (ImGui.Button("Copy segment [B]")) {
                RoadBuilderClipboard.CopiedTemplate = new RoadBuilderTemplate(new NodeSpec(Start.Lanes), new NodeSpec(End.Lanes));
                Message = "Copied the segment template";
            }
            ImGui.SameLine();
            if (ImGui.Button("Paste segment [N]")) PasteTemplate();
            ImGui.SameLine();
            if (ImGui.Button("Build [Enter]")) Build();

            ImGui.End();
        }

        private void ShowSide(string title, RoadBuilderSide side, ref int selected) {
            ImGui.Separator();
            ImGui.Text(side.IsFloating ? $"{title}: floating (editable)" : $"{title}: existing node end (read-only)");
            var LaneCount = side.Lanes.Count;
            ImGui.BeginListBox($"##{title}lanes", new(0, (Math.Min(6, LaneCount) + 0.5f) * ImGui.GetTextLineHeightWithSpacing()));
            for (int i = 0; i < LaneCount; i++) {
                ImGui.PushID(i);
                var lane = side.Lanes[i];
                if (ImGui.Selectable($"{i}: {Describe(lane)}", selected == i)) selected = i;
                ImGui.PopID();
            }
            ImGui.EndListBox();

            if (side.IsFloating) {
                if (ImGui.Button($"Add lane##{title}")) {
                    side.Lanes.Add(new LaneNode(Menu.LaneSpec, 0));
                    selected = side.Lanes.Count - 1;
                }
                if (selected >= 0 && selected < LaneCount) {
                    ImGui.SameLine();
                    if (ImGui.Button($"Remove##{title}")) {
                        side.Lanes.RemoveAt(selected);
                        selected = -1;
                    }
                    if (selected > 0) {
                        ImGui.SameLine();
                        if (ImGui.Button($"Up##{title}"))
                            (side.Lanes[selected - 1], side.Lanes[selected]) = (side.Lanes[selected], side.Lanes[selected - 1]);
                    }
                    if (selected >= 0 && selected < LaneCount - 1) {
                        ImGui.SameLine();
                        if (ImGui.Button($"Down##{title}"))
                            (side.Lanes[selected + 1], side.Lanes[selected]) = (side.Lanes[selected], side.Lanes[selected + 1]);
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button($"Copy [C]##{title}"))
                    RoadBuilderClipboard.CopiedSpec = new NodeSpec(side.Lanes);
                ImGui.SameLine();
                if (ImGui.Button($"Paste [V]##{title}")) PasteSpec(side);
            } else if (LaneCount > 0) {
                ImGui.Text("Copied specs can be pasted into floating ends only.");
                if (ImGui.Button($"Copy [C]##{title}"))
                    RoadBuilderClipboard.CopiedSpec = new NodeSpec(side.Lanes);
            }

            //Selected lane editor
            if (selected >= 0 && selected < LaneCount && side.IsFloating) {
                var lane = side.Lanes[selected];
                var spec = lane.LaneSpec;
                ImGui.PushID($"edit{title}");
                if (DearUI.InputLaneSpec($"Lane {selected}", ref spec))
                    side.Lanes[selected] = new LaneNode(spec, lane.CenterPos, lane.ID);
                ImGui.PopID();
            }
        }

        private void PasteSpec(RoadBuilderSide side) {
            var spec = RoadBuilderClipboard.CopiedSpec;
            if (spec == null) { Message = "Nothing to paste"; return; }
            if (!side.IsFloating) { Message = "Cannot paste into an existing node end"; return; }
            side.Lanes.Clear();
            side.Lanes.AddRange(RoadBuilderClipboard.Reguid(spec));
        }

        private void PasteTemplate() {
            var template = RoadBuilderClipboard.CopiedTemplate;
            if (template == null) { Message = "No segment template copied"; return; }
            PasteSpec(Start, template.Value.StartSpec);
            PasteSpec(End, template.Value.EndSpec);
            Message = "Pasted the segment template into floating ends";
        }

        private void PasteSpec(RoadBuilderSide side, NodeSpec spec) {
            if (!side.IsFloating) return;
            side.Lanes.Clear();
            side.Lanes.AddRange(RoadBuilderClipboard.Reguid(spec));
        }

        private void ShowMappingPreview() {
            var startLanes = EffectiveLanes(Start);
            var endLanes = EffectiveLanes(End);
            if (startLanes.Count == 0 || endLanes.Count == 0) {
                ImGui.Text("Add lanes on both ends to see transitions.");
                return;
            }
            var steps = NodeSpecAlignment.Align(startLanes, endLanes);
            ImGui.Text("Lane transitions (start -> end):");
            foreach (var step in steps) {
                var startDesc = step.StartIndex < 0 ? "(nothing)"
                    : step.Kind == LaneAlignmentKind.Merge
                        ? $"{step.StartIndex}+{step.StartIndex2}: {Describe(startLanes[step.StartIndex])}"
                        : $"{step.StartIndex}: {Describe(startLanes[step.StartIndex])}";
                var endDesc = step.EndIndex < 0 ? "(nothing)"
                    : step.Kind == LaneAlignmentKind.Expand
                        ? $"{step.EndIndex}+{step.EndIndex2}: {Describe(endLanes[step.EndIndex])}"
                        : $"{step.EndIndex}: {Describe(endLanes[step.EndIndex])}";
                ImGui.Text($"{step.Kind}: {startDesc} -> {endDesc}");
            }
        }

        private static string Describe(LaneNode lane) =>
            $"{lane.LaneSpec.VehicleTypes} {lane.LaneSpec.Width:0.##}m";

        //KEYS AND MOUSE

        void IMode.OnKeyPress(Key key) {
            switch (key) {
                case Key.Enter:
                    if (Phase == RoadBuilderPhase.Edit) Build();
                    break;
                case Key.T:
                    ActiveSide = 1 - ActiveSide;
                    break;
                case Key.C:
                    if (Phase == RoadBuilderPhase.Edit)
                        RoadBuilderClipboard.CopiedSpec = new NodeSpec(Active.Lanes);
                    break;
                case Key.V:
                    if (Phase == RoadBuilderPhase.Edit) PasteSpec(Active);
                    break;
            }
        }

        void IMode.OnMousePress(MouseButton button) {
            if (button == MouseButton.Right) {
                if (Phase == RoadBuilderPhase.PickEnd) {
                    End = new RoadBuilderSide();
                    Phase = RoadBuilderPhase.PickStart;
                } else if (Phase == RoadBuilderPhase.Edit) Reset();
                return;
            }
            if (button != MouseButton.Left) return;
            if (Phase == RoadBuilderPhase.PickStart) {
                var nodeEnd = PickedNodeEnd();
                if (nodeEnd != null) {
                    Start.Picked = nodeEnd;
                    Start.CaptureFromPicked();
                } else {
                    Start.Picked = null;
                    Start.FloatingPosition = PositionEulerAngles.Zero with { Position = HoverPosition() };
                    if (Start.Lanes.Count == 0) Start.Lanes.Add(new LaneNode(Menu.LaneSpec, 0));
                }
                Phase = RoadBuilderPhase.PickEnd;
                Message = "";
            } else if (Phase == RoadBuilderPhase.PickEnd) {
                var nodeEnd = PickedNodeEnd();
                if (nodeEnd != null && Start.Picked != null && nodeEnd.End == Start.Picked.End && nodeEnd.Node == Start.Picked.Node) {
                    Message = "The end must be different from the start";
                    return;
                }
                if (nodeEnd != null) {
                    End.Picked = nodeEnd;
                    End.CaptureFromPicked();
                } else {
                    End.Picked = null;
                    End.FloatingPosition = PositionEulerAngles.Zero with { Position = HoverPosition() };
                    if (End.Lanes.Count == 0) End.Lanes.Add(new LaneNode(Menu.LaneSpec, 0));
                }
                Phase = RoadBuilderPhase.Edit;
                Message = "";
            }
        }

        private RoadNodeEnd? PickedNodeEnd() => Menu.MouseOver?.As<IRoadElement>()?.GetNodeEnd();

        private Vector3 HoverPosition() {
            var plane = Menu.snappingGrid.CreateSnappingPlane();
            var position = GeometryUtils.IntersectRayPlane(Menu.MouseRay, plane);
            if (Menu.SnappingEnabled) position = Menu.snappingGrid.Snap(position);
            return position;
        }

        private void Reset() {
            Start = new RoadBuilderSide();
            End = new RoadBuilderSide();
            Phase = RoadBuilderPhase.PickStart;
            Message = "";
        }

        void IMode.WorldChanged(TSWorld world) => Reset();

        //BUILDING

        /// <summary>Lanes used for alignment: packed for floating sides, as captured for existing ones.</summary>
        private List<LaneNode> EffectiveLanes(RoadBuilderSide side) =>
            side.IsFloating ? PackLanes(side.Lanes) : side.Lanes.ToList();

        /// <summary>Packs the lanes left to right, centered around 0, without overlaps.</summary>
        private static List<LaneNode> PackLanes(IReadOnlyList<LaneNode> lanes) {
            var result = new List<LaneNode>(lanes.Count);
            var total = lanes.Sum(l => l.LaneSpec.Width);
            var x = -total / 2;
            foreach (var lane in lanes) {
                var width = lane.LaneSpec.Width;
                result.Add(new LaneNode(lane.LaneSpec, x + width / 2, lane.ID));
                x += width;
            }
            return result;
        }

        /// <summary>
        /// Orders the half-lanes of an existing end left to right in the world lateral frame of the segment.
        /// </summary>
        private static List<HalfLane> OrderedHalfLanes(RoadNodeEnd picked, Vector3 worldLateral) {
            var frame = picked.CalcReferenceFrame();
            var halfLanes = picked.HalfNode.GetLaneList().OrderBy(x => x.MiddlePosition).ToList();
            if (Vector3.Dot(frame.X, worldLateral) < 0) halfLanes.Reverse();
            return halfLanes;
        }

        private void Build() {
            if (Start.Lanes.Count == 0 || End.Lanes.Count == 0) {
                Message = "Both ends need at least one lane";
                return;
            }
            var world = Menu.World;

            //Resolve the positions and the segment direction
            var startPos = Start.IsFloating ? Start.FloatingPosition.Position : Start.Picked.CalcReferenceFrame().O;
            var endPos = End.IsFloating ? End.FloatingPosition.Position : End.Picked.CalcReferenceFrame().O;
            var delta = endPos - startPos;
            if (delta.LengthSquared() < 1e-6f || !delta.IsFinite()) {
                Message = "The two ends coincide or are invalid";
                return;
            }
            var dir = Vector3.Normalize(delta);
            var lateral = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
            if (!lateral.IsFinite()) lateral = Vector3.UnitX;

            //Resolve the start half-node (the half the strip attaches to)
            HalfNode startNode;
            if (Start.IsFloating) {
                var nodePosition = PositionEulerAngles.FromPosTangentLateral(Start.FloatingPosition.Position, dir, lateral);
                var node = new RoadNode("", nodePosition);
                startNode = node.FrontHalf;
                foreach (var lane in PackLanes(Start.Lanes)) startNode.AddLane(lane);
                world.Nodes.data.Add(node);
            } else startNode = Start.Picked.HalfNode;

            //Resolve the end half-node. For floating ends, lanes are built on the front half
            //(aligned with the segment direction) and the strip attaches to the opposite half.
            HalfNode endLanesHalf;
            if (End.IsFloating) {
                var nodePosition = PositionEulerAngles.FromPosTangentLateral(End.FloatingPosition.Position, dir, lateral);
                var node = new RoadNode("", nodePosition);
                endLanesHalf = node.FrontHalf;
                foreach (var lane in PackLanes(End.Lanes)) endLanesHalf.AddLane(lane);
                world.Nodes.data.Add(node);
            } else endLanesHalf = End.Picked.HalfNode;

            //Ordered lane lists in a common lateral frame
            var startHalfLanes = Start.IsFloating
                ? startNode.GetLaneList().OrderBy(x => x.MiddlePosition).ToList()
                : OrderedHalfLanes(Start.Picked, lateral);
            var startLanes = startHalfLanes.Select(x => x.LaneNode).ToList();
            List<HalfLane> endHalfLanes;
            List<LaneNode> endLanes;
            if (End.IsFloating) {
                endHalfLanes = endLanesHalf.GetLaneList().OrderBy(x => x.MiddlePosition).ToList();
                endLanes = PackLanes(End.Lanes);
            } else {
                endHalfLanes = OrderedHalfLanes(End.Picked, lateral);
                endLanes = endHalfLanes.Select(x => x.LaneNode).ToList();
            }

            //Align the specs and convert to lane mappings
            var steps = NodeSpecAlignment.Align(startLanes, endLanes);
            var mappings = NodeSpecAlignment.ToLaneMappings(steps, startLanes, endLanes);
            if (mappings.Count == 0) {
                Message = "No lane transitions between these specs";
                return;
            }

            //Build the road strip. For floating ends the strip attaches to the opposite half.
            var endFacing = End.IsFloating ? endLanesHalf.OppositeHalf : endLanesHalf;
            var road = world.GetOrMakeRoadStrip(startNode, endFacing, Menu.RoadFinish);
            var endBackward = endFacing.OppositeHalf.End == NodeEnd.Backward;
            foreach (var mapping in mappings) {
                var startLane = startHalfLanes[mapping.StartIndex];
                var endLane = End.IsFloating ? endHalfLanes[mapping.EndIndex].OppositeHalf : endHalfLanes[mapping.EndIndex];

                var isBackwardsToBuildDirection = Menu.SegmentPresets.DirectionChoice switch {
                    DirectionChoice.Auto => LaneMappings.IsReverseLaneHeuristic(startLane.Lane) ^ endBackward,
                    DirectionChoice.Forward => false,
                    DirectionChoice.Reverse => true,
                    _ => false
                };
                var isBackwards = isBackwardsToBuildDirection ^ endBackward;
                if (isBackwardsToBuildDirection) DataUtil.Swap(ref startLane, ref endLane);
                var spec = mapping.LaneSpec;
                if (isBackwards) spec.Flags = spec.Flags.LongitudinalReverse();
                road.AddLaneStrip(new LaneStrip(startLane, endLane, spec));
            }
            world.RoadSegments.data.Add(road);

            //Continue building from the built end
            var continueFrom = End.IsFloating ? endLanesHalf.RoadNode.GetEnd(NodeEnd.Forward) : End.Picked;
            Start = new RoadBuilderSide();
            if (continueFrom != null) {
                Start.Picked = continueFrom;
                Start.CaptureFromPicked();
            }
            End = new RoadBuilderSide();
            SelectedStartLane = SelectedEndLane = -1;
            Phase = RoadBuilderPhase.PickEnd;
            Message = "Segment built. Continue from the end node, or [RMB] to reset.";
        }

        //PREVIEW

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            if (Phase == RoadBuilderPhase.PickStart) return;
            var pickingEnd = Phase == RoadBuilderPhase.PickEnd;

            if (!ResolvePosition(Start, false, out var startPos)) return;
            if (!ResolvePosition(End, pickingEnd, out var endPos)) return;
            var delta = endPos - startPos;
            if (delta.LengthSquared() < 1e-6f || !delta.IsFinite()) return;
            var dir = Vector3.Normalize(delta);
            var worldLateral = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
            if (!worldLateral.IsFinite()) worldLateral = Vector3.UnitX;

            if (!ResolveSide(Start, worldLateral, out var startLateral, out var startLanes)) return;
            if (!ResolveSide(End, worldLateral, out var endLateral, out var endLanes)) return;

            var accuracy = Math.Min(Settings.RoadAccuracy, 16);
            var up = Vector3.UnitY * 0.05f;

            //Asphalt preview band between the spec ranges
            var asphaltBin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Asphalt);
            var previewColor = new Color(64, 64, 64, 128);
            var startRange = LaneBounds(startLanes);
            var endRange = LaneBounds(endLanes);
            var left = new Vector3[accuracy];
            var right = new Vector3[accuracy];
            for (int i = 0; i < accuracy; i++) {
                var t = i / (accuracy - 1f);
                var p = Vector3.Lerp(startPos, endPos, t) + Vector3.UnitY * 0.02f;
                var lateral = Vector3.Normalize(Vector3.Lerp(startLateral, endLateral, t) + worldLateral * 0.001f);
                var min = startRange.Min + (endRange.Min - startRange.Min) * t;
                var max = startRange.Max + (endRange.Max - startRange.Max) * t;
                left[i] = p + lateral * min;
                right[i] = p + lateral * max;
            }
            asphaltBin.DrawStrip(UniformTexturing.UniformTexturedTwin(left, right,
                UniformTexturing.GenerateLaneStripVertexGen(previewColor)));

            //Lane bars at both ends
            DrawLaneBars(renderMeshPool, startPos, startLateral, startLanes, up);
            DrawLaneBars(renderMeshPool, endPos, endLateral, endLanes, up);

            //Node direction markers
            DrawNodeMarker(renderMeshPool, startPos, Start, dir, up);
            if (!pickingEnd) DrawNodeMarker(renderMeshPool, endPos, End, -dir, up);

            //Lane transition connectors
            if (pickingEnd) return;
            var steps = NodeSpecAlignment.Align(startLanes, endLanes);
            var connectorBin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Arrow);
            foreach (var step in steps) {
                if (step.Kind == LaneAlignmentKind.Terminated || step.Kind == LaneAlignmentKind.Spawned) continue;
                int[] sources = step.Kind == LaneAlignmentKind.Merge
                    ? [step.StartIndex, step.StartIndex2] : [step.StartIndex];
                int[] targets = step.Kind == LaneAlignmentKind.Expand
                    ? [step.EndIndex, step.EndIndex2] : [step.EndIndex];
                foreach (var s in sources) {
                    foreach (var e in targets) {
                        var a = startPos + startLateral * startLanes[s].CenterPos + Vector3.UnitY * 0.08f;
                        var b = endPos + endLateral * endLanes[e].CenterPos + Vector3.UnitY * 0.08f;
                        connectorBin.DrawLine(a, b, Vector3.UnitY, endLanes[e].LaneSpec.Color, 0.25f);
                    }
                }
            }
        }

        private void DrawLaneBars(MultiMesh renderMeshPool, Vector3 pos, Vector3 lateral, List<LaneNode> lanes, Vector3 up) {
            var bin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Road);
            foreach (var lane in lanes) {
                var a = pos + lateral * lane.Bounds.Min + up;
                var b = pos + lateral * lane.Bounds.Max + up;
                bin.DrawLine(a, b, Vector3.UnitY, lane.LaneSpec.Color, 0.4f);
            }
        }

        private void DrawNodeMarker(MultiMesh renderMeshPool, Vector3 pos, RoadBuilderSide side, Vector3 dir, Vector3 up) {
            var bin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Road);
            var tangent = side.IsFloating ? dir : side.Picked.CalcReferenceFrame().Z;
            bin.DrawLine(pos, pos + tangent * 2, Vector3.UnitY, Colors.Red, 0.1f);
            bin.DrawLine(pos, pos - tangent * 2, Vector3.UnitY, Colors.Maroon, 0.1f);
        }

        private bool ResolvePosition(RoadBuilderSide side, bool useHover, out Vector3 pos) {
            if (side.IsFloating) {
                pos = useHover ? HoverPosition() : side.FloatingPosition.Position;
                return pos.IsFinite();
            }
            if (side.Picked == null) {
                pos = default;
                return false;
            }
            pos = side.Picked.CalcReferenceFrame().O;
            return pos.IsFinite();
        }

        /// <summary>
        /// Resolves the lateral direction and effective lanes of one end, aligned with the
        /// world lateral direction of the segment. Existing ends keep their own lane frame,
        /// with the lane order and lateral flipped when it points against the segment lateral.
        /// </summary>
        private bool ResolveSide(RoadBuilderSide side, Vector3 worldLateral, out Vector3 lateral, out List<LaneNode> lanes) {
            lateral = Vector3.UnitX;
            lanes = new List<LaneNode>();
            if (side.IsFloating) {
                lateral = worldLateral;
            } else {
                if (side.Picked == null) return false;
                var frame = side.Picked.CalcReferenceFrame();
                lateral = frame.X;
                if (Vector3.Dot(lateral, worldLateral) < 0) lateral = -lateral;
            }
            lanes = EffectiveLanes(side);
            if (lanes.Count == 0) return false;
            //Mirror the lane order for anti-aligned existing ends, so that the alignment
            //and the preview stay consistent with the order used when building
            if (!side.IsFloating && Vector3.Dot(side.Picked.CalcReferenceFrame().X, worldLateral) < 0)
                lanes.Reverse();
            return true;
        }

        private static Interval<float> LaneBounds(List<LaneNode> lanes) {
            var range = new Interval<float>(0, 0);
            foreach (var lane in lanes) range = range.Union(lane.Bounds);
            return range;
        }
    }
}
