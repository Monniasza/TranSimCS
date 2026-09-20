using System;
using System.Diagnostics;
using System.Numerics;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet;
using TranSimCS.Worlds;

namespace TranSimCS.Mode.RoadBuilder {
    /// <summary>
    /// Draws the Road Builder's 3D preview.
    /// <para>
    /// The preview is a real <see cref="RoadNode"/> built from the draft and drawn through the production
    /// <see cref="NodeRenderer"/> path, so what the user sees is what gets placed. The node is a throwaway
    /// that never enters the world; it is cached and rebuilt only when the draft changes, because
    /// rebuilding it every frame would be wasteful.
    /// </para>
    /// </summary>
    public sealed class RoadBuilderRenderer {
        private readonly SilkNetTest menu;

        //The throwaway node, cached against the draft it was built from.
        private RoadNode? previewNode;
        private NodeSpecDraft? previewSource;
        private int previewVersion = -1;

        /// <summary>
        /// Bumped whenever the draft is mutated, so the cached preview node can be rebuilt. The tool
        /// increments this on every edit.
        /// </summary>
        public int GeometryVersion { get; private set; }

        /// <summary>
        /// The material used for the pickable lane and gap regions. Defaults to
        /// <see cref="Materials.Add"/>, the same material the existing add-lane selectors use.
        /// <para>
        /// The default is resolved lazily because <see cref="Materials.Add"/> is only populated once
        /// <see cref="Materials.ReadAssets"/> has run, which happens after the modes are constructed.
        /// </para>
        /// </summary>
        public SimpleMaterial SelectorMaterial { get; set; }

        public RoadBuilderRenderer(SilkNetTest menu) {
            this.menu = menu;
            SelectorMaterial = Materials.Add;
        }

        /// <summary>Marks the cached preview node as stale.</summary>
        public void Invalidate() => GeometryVersion++;

        /// <summary>
        /// The throwaway node for the given draft, rebuilt if the draft has changed since the last call.
        /// Returns <see langword="null"/> when there is nothing to preview.
        /// </summary>
        public RoadNode? GetPreviewNode(NodeSpecDraft? draft, HalfNode? atEnd) {
            if (draft == null || atEnd == null || draft.Count == 0) return null;

            if (previewNode == null || !ReferenceEquals(previewSource, draft) || previewVersion != GeometryVersion) {
                previewNode = BuildPreviewNode(draft, atEnd);
                previewSource = draft;
                previewVersion = GeometryVersion;
            }
            return previewNode;
        }

        /// <summary>
        /// Builds a detached <see cref="RoadNode"/> from the draft, positioned at the source end's
        /// reference frame. The node is not added to any world.
        /// </summary>
        private static RoadNode BuildPreviewNode(NodeSpecDraft draft, HalfNode atEnd) {
            var position = atEnd.RoadNode.PositionProp.Value; //the position comes from the start node
            var node = new RoadNode("Road Builder preview", position);
            foreach (var laneNode in draft.ToNodeSpec().Lanes)
                node.AddLane(laneNode);
            return node;
        }

        /// <summary>
        /// Draws the draft preview, the lane hit regions, and the mapping connectors.
        /// </summary>
        /// <param name="state">The current tool state.</param>
        /// <param name="target">Render target for instanced meshes.</param>
        /// <param name="renderMeshPool">Render target for dynamically generated geometry.</param>
        public void Draw(RoadBuilderState state, RenderTarget target, MultiMesh renderMeshPool) {
            var draft = state.Draft;
            var atEnd = state.SourceHalfNode;
            if (draft == null || atEnd == null) return;

            var node = GetPreviewNode(draft, atEnd);
            if (node == null) return;

            //Draw the draft's lanes through the production renderer, so the preview matches the result.
            var material = Materials.Asphalt;
            material.BlendMode = MaterialBlendMode.Transparent;
            var bin = renderMeshPool.GetOrCreateRenderBinForced(material);
            NodeRenderer.GenerateRoadNodeSelectionMesh(node, bin, null);

            //Draw the mapping connectors while connecting.
            if (state.Phase == RoadBuilderPhase.Connecting && state.Mapping != null) //Phase Connecting not set
                DrawMappingConnectors(state, renderMeshPool);
        }

        /// <summary>
        /// Draws a connector for each matched lane pair, so the user can see how the source lanes map onto
        /// the draft before committing.
        /// </summary>
        private void DrawMappingConnectors(RoadBuilderState state, MultiMesh renderMeshPool) {
            var mapping = state.Mapping!;
            var sourceDraft = state.SourceDraft;
            var draft = state.Draft;
            var atEnd = state.SourceHalfNode;
            var destNode = GetPreviewNode(draft, atEnd);
            if (sourceDraft == null || draft == null || atEnd == null) return;
            Debug.Assert(destNode != null, "destNode not set");

            var srcFrame = atEnd.Cache.ReferenceFrame;
            var destFrame = destNode.Cache.ReferenceFrame;
            //srcFrame == destFrame somehow

            var bin = renderMeshPool.GetOrCreateRenderBinForced(Materials.Arrow);

            var sourceOffsets = sourceDraft.ComputeOffsets();
            var destOffsets = draft.ComputeOffsets();

            foreach (var pair in mapping.Matched) {
                var sourceIndex = sourceDraft.IndexOf(pair.Source);
                var destIndex = draft.IndexOf(pair.Dest);
                if (sourceIndex < 0 || destIndex < 0) continue;

                var sourcePos = srcFrame.O + srcFrame.X * sourceOffsets[sourceIndex];
                var destPos = destFrame.O + destFrame.X * destOffsets[destIndex];
                var color = draft[destIndex].Spec.Color;
                bin.DrawLine(sourcePos, destPos, destFrame.Y, color, 0.2f);
            }
        }

        /// <summary>
        /// Draws the pickable regions for the draft's lanes and the gaps between them, tagging each with a
        /// <see cref="LaneHit"/> so the picker can report which lane the cursor is over.
        /// </summary>
        /// <param name="state">The current tool state.</param>
        /// <param name="visible">The visible selector target.</param>
        public void AddSelectors(RoadBuilderState state, MultiMesh visible) {
            var draft = state.Draft;
            var atEnd = state.SourceHalfNode;
            if (draft == null || atEnd == null || draft.Count == 0) return;

            var node = GetPreviewNode(draft, atEnd);
            if (node == null) return;

            var bin = visible.GetOrCreateRenderBinForced(SelectorMaterial);
            var bounds = draft.ComputeBounds();
            var refframe = atEnd.RoadNode.ReferenceFrame;

            //One pickable quad per lane.
            for (int i = 0; i < draft.Count; i++) {
                var laneBounds = bounds[i];
                var hit = new LaneHit(draft[i].Id, i, laneBounds.Middle(), atEnd);
                DrawHitQuad(bin, node, laneBounds, hit);
            }

            //One pickable quad per gap, so a lane can be inserted between any two lanes.
            for (int i = 0; i <= draft.Count; i++) {
                var gap = ComputeGap(bounds, i);
                if (gap.Width() <= 0) continue;
                var hit = LaneHit.Insertion(i, gap.Middle(), atEnd);
                DrawHitQuad(bin, node, gap, hit);
            }
        }

        /// <summary>
        /// The cross-section extent of the gap at <paramref name="index"/>, where a new lane would be
        /// inserted. The gap is a fixed-width band centred on the boundary between the neighbouring lanes.
        /// </summary>
        private static Interval<float> ComputeGap(Interval<float>[] bounds, int index) {
            const float gapWidth = 0.5f;
            float center;
            if (index == 0) center = bounds[0].Min - gapWidth / 2;
            else if (index == bounds.Length) center = bounds[^1].Max + gapWidth / 2;
            else center = (bounds[index - 1].Max + bounds[index].Min) / 2;
            return new(center - gapWidth / 2, center + gapWidth / 2);
        }

        /// <summary>
        /// Draws one pickable quad across the node's cross-section and tags it with the given hit.
        /// </summary>
        private static void DrawHitQuad(Mesh mesh, RoadNode node, Interval<float> range, LaneHit hit) {
            var quad = NodeRenderer.GenerateLaneQuad(node, range.Min, range.Max, new Color(128, 128, 128, 64), 0.2f, -1, 1);
            mesh.DrawQuad(quad);
            mesh.AddTagsToLastTriangles(2, hit);
        }
    }
}
