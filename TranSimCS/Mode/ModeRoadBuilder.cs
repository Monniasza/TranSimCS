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
using TranSimCS.Spline;
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

        /// <summary>Lanes used by the mapping preview: packed for floating sides, as captured for existing ones.</summary>
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

        /// <summary>
        /// Per-side data resolved from a <see cref="RoadBuilderSide"/>, shared by <see cref="Build"/>
        /// and the preview. Lane bounds are kept in the attaching half's frame (the convention of the
        /// road strip geometry), while <see cref="AlignmentLanes"/> carries world-frame lane proxies
        /// for the alignment, so both ends live in one common lateral frame.
        /// </summary>
        private class ResolvedSide {
            public bool IsExisting;
            public HalfNode AttachHalf;
            public Transform3 AttachFrame;
            public PositionEulerAngles NodeEuler;
            /// <summary>Half-lanes whose <see cref="LaneStrip"/> endpoints the strip attaches to, world L→R.</summary>
            public List<HalfLane> StripLanes = new();
            /// <summary>Floating ends only: the lanes packed in the frame of the half they are created on.</summary>
            public List<LaneNode> PackedLanes = new();
            public List<LaneNode> AlignmentLanes = new();
            /// <summary>Per-lane bounds in the attaching half's frame (the road strip geometry convention).</summary>
            public List<Interval<float>> LaneBounds = new();
            public Interval<float> Extent;

            public LaneSpec Spec(int index) =>
                IsExisting ? StripLanes[index].LaneNode.LaneSpec : PackedLanes[index].LaneSpec;
            public Interval<float> Bounds(int index) => LaneBounds[index];
        }

        /// <summary>
        /// Resolves one end into <see cref="ResolvedSide"/>: the attaching half-node (for existing ends
        /// the picked half itself, for floating ends the half synthesized the same way Build creates it),
        /// its frame, and the lane list ordered left to right in the world lateral frame.
        /// </summary>
        private bool ResolveSide(RoadBuilderSide side, bool isStart, Vector3 dir, Vector3 worldLateral,
            Vector3? overrideFloatingPos, ResolvedSide resolved) {
            resolved.IsExisting = false;
            resolved.StripLanes.Clear();
            resolved.PackedLanes.Clear();
            resolved.AlignmentLanes.Clear();
            resolved.LaneBounds.Clear();
            resolved.Extent = default;

            if (side.IsFloating) {
                var pos = overrideFloatingPos ?? side.FloatingPosition.Position;
                if (!pos.IsFinite()) return false;
                resolved.NodeEuler = PositionEulerAngles.FromPosTangentLateral(pos, dir, worldLateral);
                var nodeFrame = resolved.NodeEuler.CalcReferenceFrame();
                //The strip attaches to the front half at the start, and to the opposite (backward)
                //half at the end, whose frame mirrors the lane packing.
                resolved.AttachFrame = isStart ? nodeFrame : nodeFrame.Around();
                resolved.PackedLanes.AddRange(PackLanes(side.Lanes));
                for (int i = 0; i < resolved.PackedLanes.Count; i++) {
                    var lane = resolved.PackedLanes[i];
                    //Packed coordinates live in the node (front half) frame
                    var attachBounds = isStart ? lane.Bounds : new Interval<float>(-lane.Bounds.Max, -lane.Bounds.Min);
                    var worldPos = Vector3.Dot(nodeFrame.X, worldLateral) * lane.CenterPos;
                    resolved.AlignmentLanes.Add(new LaneNode(lane.LaneSpec, worldPos, lane.ID));
                    resolved.LaneBounds.Add(attachBounds);
                    resolved.Extent = i == 0 ? attachBounds : resolved.Extent.Union(attachBounds);
                }
            } else {
                if (side.Picked == null) return false;
                var attachHalf = side.Picked.HalfNode;
                if (attachHalf == null) return false;
                resolved.IsExisting = true;
                resolved.AttachHalf = attachHalf;
                resolved.AttachFrame = attachHalf.Cache.ReferenceFrame;
                resolved.NodeEuler = side.Picked.PositionProp.Value;
                foreach (var halfLane in OrderedHalfLanes(side.Picked, worldLateral)) {
                    resolved.StripLanes.Add(halfLane);
                    var worldPos = Vector3.Dot(resolved.AttachFrame.X, worldLateral) * halfLane.MiddlePosition;
                    resolved.AlignmentLanes.Add(new LaneNode(halfLane.LaneNode.LaneSpec, worldPos, halfLane.LaneNode.ID));
                    resolved.LaneBounds.Add(halfLane.Bounds);
                    resolved.Extent = resolved.LaneBounds.Count == 1
                        ? halfLane.Bounds : resolved.Extent.Union(halfLane.Bounds);
                }
            }
            return resolved.AlignmentLanes.Count > 0;
        }

        private bool ResolveFrames(Vector3? hoverEnd, ResolvedSide startSide, ResolvedSide endSide) {
            if (!ResolvePosition(Start, false, out var startPos)) return false;
            if (!ResolvePosition(End, hoverEnd != null, out var endPos)) return false;
            var delta = endPos - startPos;
            if (delta.LengthSquared() < 1e-6f || !delta.IsFinite()) return false;
            var dir = Vector3.Normalize(delta);
            var worldLateral = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
            if (!worldLateral.IsFinite()) worldLateral = Vector3.UnitX;
            if (!ResolveSide(Start, true, dir, worldLateral, null, startSide)) return false;
            if (!ResolveSide(End, false, dir, worldLateral, hoverEnd, endSide)) return false;
            return true;
        }

        private void Build() {
            if (Start.Lanes.Count == 0 || End.Lanes.Count == 0) {
                Message = "Both ends need at least one lane";
                return;
            }
            var world = Menu.World;

            var startSide = new ResolvedSide();
            var endSide = new ResolvedSide();
            if (!ResolveFrames(null, startSide, endSide)) {
                Message = "The two ends coincide or are invalid";
                return;
            }

            //Create the floating nodes. Floating ends keep their lanes on the front half;
            //the start strip attaches to it directly and the end strip to the opposite half.
            if (!startSide.IsExisting) {
                var node = new RoadNode("", startSide.NodeEuler);
                var attach = node.FrontHalf;
                foreach (var laneNode in startSide.PackedLanes) attach.AddLane(laneNode);
                world.Nodes.data.Add(node);
                startSide.AttachHalf = attach;
                startSide.StripLanes.AddRange(attach.GetLaneList().OrderBy(x => x.MiddlePosition));
            }
            if (!endSide.IsExisting) {
                var node = new RoadNode("", endSide.NodeEuler);
                var lanesHalf = node.FrontHalf;
                foreach (var laneNode in endSide.PackedLanes) lanesHalf.AddLane(laneNode);
                world.Nodes.data.Add(node);
                endSide.AttachHalf = lanesHalf.OppositeHalf;
                endSide.StripLanes.AddRange(lanesHalf.GetLaneList().OrderBy(x => x.MiddlePosition));
            }

            //Align the specs and convert to lane mappings, in the world lateral frame
            var startAlign = startSide.AlignmentLanes;
            var endAlign = endSide.AlignmentLanes;
            var steps = NodeSpecAlignment.Align(startAlign, endAlign);
            var mappings = NodeSpecAlignment.ToLaneMappings(steps, startAlign, endAlign);
            if (mappings.Count == 0) {
                Message = "No lane transitions between these specs";
                return;
            }

            //Build the road strip
            var road = world.GetOrMakeRoadStrip(startSide.AttachHalf, endSide.AttachHalf, Menu.RoadFinish);
            var endBackward = endSide.AttachHalf.OppositeHalf.End == NodeEnd.Backward;
            foreach (var mapping in mappings) {
                var startLane = startSide.StripLanes[mapping.StartIndex];
                var endLane = endSide.StripLanes[mapping.EndIndex];
                if (!endSide.IsExisting) endLane = endLane.OppositeHalf;

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
            var continueFrom = endSide.IsExisting ? End.Picked : endSide.AttachHalf.OppositeHalf.RoadNode.GetEnd(NodeEnd.Forward);
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

        private bool ResolvePreview(ResolvedSide startSide, ResolvedSide endSide) {
            var hoveringEnd = Phase == RoadBuilderPhase.PickEnd;
            if (!ResolvePosition(Start, false, out var startPos)) return false;
            if (!ResolvePosition(End, hoveringEnd, out var endPos)) return false;
            return ResolveFrames(hoveringEnd ? endPos : null, startSide, endSide);
        }

        void IMode.Draw3D(RenderTarget target, MultiMesh renderMeshPool) {
            if (Phase == RoadBuilderPhase.PickStart) return;
            var pickingEnd = Phase == RoadBuilderPhase.PickEnd;

            var startSide = new ResolvedSide();
            var endSide = new ResolvedSide();
            try {
                if (!ResolvePreview(startSide, endSide)) return;
                DrawPreview(renderMeshPool, startSide, endSide, pickingEnd);
            } catch (Exception e) when (e is ArgumentException or InvalidOperationException) {
                //Degenerate geometry (e.g. hovering at the start point) - skip this preview frame
                Message = e.Message;
            }
        }

        private void DrawPreview(MultiMesh renderMeshPool, ResolvedSide startSide, ResolvedSide endSide, bool pickingEnd) {

            //Replicate the road strip geometry: anisotropic center spline between the attaching
            //halves, then orthodistant offset curves for the band edges (as RoadStrip does).
            var accuracy = Settings.RoadAccuracy;
            var startMiddle = (startSide.Extent.Min + startSide.Extent.Max) / 2;
            var endMiddle = (endSide.Extent.Min + endSide.Extent.Max) / 2;
            var indexStrip = SplineAlgorithms.GenerateSegmentSplinedUsingAlg(
                startSide.AttachFrame, endSide.AttachFrame, new(startMiddle, endMiddle),
                SplineAlgorithms.AnisotropicSpline);
            var basis = indexStrip.ToOrthodistantBasis(startSide.NodeEuler, endSide.NodeEuler);

            Vector3[] Curve(float startT, float endT, float y) {
                var result = new Vector3[accuracy];
                float step = 1 / (accuracy - 1.0f);
                for (int i = 0; i < accuracy; i++) {
                    var t = i * step;
                    result[i] = basis.SamplePosition(t,
                        new Vector3(startT, y, 0), new Vector3(endT, y, 0) * new Vector3(-1, 1, -1));
                }
                return result;
            }

            //Asphalt band over the full spec extent
            var asphaltBin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Asphalt);
            asphaltBin.DrawStrip(UniformTexturing.UniformTexturedTwin(
                Curve(startSide.Extent.Min, endSide.Extent.Max, 0.02f),
                Curve(startSide.Extent.Max, endSide.Extent.Min, 0.02f),
                UniformTexturing.GenerateLaneStripVertexGen(new Color(64, 64, 64, 128))));

            if (pickingEnd) return;

            //Per-lane strips along the alignment transitions
            var steps = NodeSpecAlignment.Align(startSide.AlignmentLanes, endSide.AlignmentLanes);
            var laneBin = renderMeshPool.GetOrCreateRenderBinForced(Materials.WhiteTransparent);
            foreach (var step in steps) {
                if (step.Kind == LaneAlignmentKind.Terminated || step.Kind == LaneAlignmentKind.Spawned) continue;
                int[] sources = step.Kind == LaneAlignmentKind.Merge
                    ? [step.StartIndex, step.StartIndex2] : [step.StartIndex];
                int[] targets = step.Kind == LaneAlignmentKind.Expand
                    ? [step.EndIndex, step.EndIndex2] : [step.EndIndex];
                foreach (var s in sources) {
                    foreach (var e in targets) {
                        var color = startSide.Spec(s).Color;
                        var tint = new Color(color.R, color.G, color.B, 140);
                        laneBin.DrawStrip(UniformTexturing.UniformTexturedTwin(
                            Curve(startSide.Bounds(s).Min, endSide.Bounds(e).Max, 0.04f),
                            Curve(startSide.Bounds(s).Max, endSide.Bounds(e).Min, 0.04f),
                            UniformTexturing.GenerateLaneStripVertexGen(tint)));
                    }
                }
            }

            //Lane bars and direction markers at both ends
            DrawLaneBars(renderMeshPool, startSide);
            DrawLaneBars(renderMeshPool, endSide);
            DrawNodeMarker(renderMeshPool, startSide);
            DrawNodeMarker(renderMeshPool, endSide);
        }

        private void DrawLaneBars(MultiMesh renderMeshPool, ResolvedSide side) {
            var bin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Road);
            for (int i = 0; i < side.AlignmentLanes.Count; i++) {
                var frame = side.AttachFrame;
                var a = frame.O + frame.X * side.Bounds(i).Min + frame.Y * 0.05f;
                var b = frame.O + frame.X * side.Bounds(i).Max + frame.Y * 0.05f;
                bin.DrawLine(a, b, frame.Y, side.Spec(i).Color, 0.4f);
            }
        }

        private void DrawNodeMarker(MultiMesh renderMeshPool, ResolvedSide side) {
            var bin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Road);
            var frame = side.AttachFrame;
            bin.DrawLine(frame.O, frame.O + frame.Z * 2, frame.Y, Colors.Red, 0.1f);
            bin.DrawLine(frame.O, frame.O - frame.Z * 2, frame.Y, Colors.Maroon, 0.1f);
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
    }
}
