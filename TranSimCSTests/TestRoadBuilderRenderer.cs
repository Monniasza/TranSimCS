using TranSimCS;
using TranSimCS.Mode.RoadBuilder;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.SilkNet;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    /// <summary>
    /// Covers the parts of <see cref="RoadBuilderRenderer"/> that do not need a GPU context: building the
    /// throwaway preview node, caching it against the draft, and generating the pickable
    /// <see cref="LaneHit"/> regions.
    /// <para>
    /// The actual drawing calls need an OpenGL context and are exercised by running the app, not here.
    /// </para>
    /// </summary>
    public class TestRoadBuilderRenderer {
        private static LaneSpec Spec(float width = 3f, VehicleTypes types = VehicleTypes.Car)
            => new(Colors.Gray, types, width, 50);

        private static RoadNode MakeNode(params float[] centers) {
            var node = new RoadNode("node", PositionEulerAngles.Zero);
            foreach (var center in centers) node.AddLane(new LaneNode(Spec(), center));
            return node;
        }

        /// <summary>
        /// A renderer with no window. The renderer only stores the menu reference, so a null is fine for
        /// the paths under test.
        /// <para>
        /// The selector material is replaced with one built from a synthetic texture, because the real
        /// <see cref="Materials.Add"/> is only populated once <c>Materials.ReadAssets</c> has run, and
        /// <c>GetOrCreateRenderBinForced</c> rejects a material with no texture. Loading a real texture
        /// would need <c>Program.DataRoot</c>, which is not set in a test host.
        /// </para>
        /// </summary>
        private static RoadBuilderRenderer MakeRenderer() {
            var texture = new TextureData(1, 1, TextureFormat.RGBA8, new byte[4]);
            return new(null!) {
                SelectorMaterial = new SimpleMaterial { Texture = texture, Emissive = texture }
            };
        }

        // --- Preview node --------------------------------------------------------------------------

        [Fact]
        public void PreviewNodeIsBuiltFromTheDraft() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var preview = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            Assert.NotNull(preview);
            Assert.Equal(3, preview!.Lanes.Count);
        }

        [Fact]
        public void PreviewNodeIsDetachedFromTheWorld() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var preview = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            //The preview must never be the source node itself, or editing the draft would edit the world.
            Assert.NotSame(node, preview);
            Assert.Equal(3, node.Lanes.Count);
        }

        [Fact]
        public void PreviewNodeIsCachedUntilInvalidated() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var first = renderer.GetPreviewNode(state.Draft, state.SourceEnd);
            var second = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            //Rebuilding every frame would be wasteful, so the node is cached.
            Assert.Same(first, second);
        }

        [Fact]
        public void InvalidatingRebuildsThePreviewNode() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var first = renderer.GetPreviewNode(state.Draft, state.SourceEnd);
            renderer.Invalidate();
            var second = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            Assert.NotSame(first, second);
        }

        [Fact]
        public void PreviewNodeReflectsDraftEditsAfterInvalidation() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var before = renderer.GetPreviewNode(state.Draft, state.SourceEnd);
            Assert.Equal(3, before!.Lanes.Count);

            state.Draft!.Add(Spec(2f, VehicleTypes.Bicycle));
            renderer.Invalidate();
            var after = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            Assert.Equal(4, after!.Lanes.Count);
        }

        [Fact]
        public void PreviewNodeIsNullWithoutADraft() {
            var renderer = MakeRenderer();
            Assert.Null(renderer.GetPreviewNode(null, null));
        }

        [Fact]
        public void PreviewNodeIsNullForAnEmptyDraft() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            state.Draft!.Remove(state.Draft[0].Id);
            state.Draft.Remove(state.Draft[0].Id);
            state.Draft.Remove(state.Draft[0].Id);
            var renderer = MakeRenderer();

            Assert.Null(renderer.GetPreviewNode(state.Draft, state.SourceEnd));
        }

        [Fact]
        public void PreviewNodeIsPositionedAtTheSourceEnd() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();

            var preview = renderer.GetPreviewNode(state.Draft, state.SourceEnd);

            Assert.Equal(node.PositionProp.Value, preview!.PositionProp.Value);
        }

        // --- Selectors -----------------------------------------------------------------------------

        [Fact]
        public void SelectorsCoverEveryLaneAndEveryGap() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            //Three lanes plus four gaps (before, between, between, after).
            var tags = CollectTags(visible);
            Assert.Equal(3 + 4, tags.Count);
            Assert.Equal(3, tags.Count(t => !t.IsInsertion));
            Assert.Equal(4, tags.Count(t => t.IsInsertion));
        }

        [Fact]
        public void LaneSelectorsCarryTheDraftLaneIdentities() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            var laneHits = CollectTags(visible).Where(t => !t.IsInsertion).ToList();
            var expected = state.Draft!.Select(x => x.Id).ToList();
            Assert.Equal(expected, laneHits.Select(t => t.Id).ToList());
        }

        [Fact]
        public void InsertionSelectorsCoverEveryIndex() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            var indices = CollectTags(visible).Where(t => t.IsInsertion).Select(t => t.Index).OrderBy(x => x).ToList();
            Assert.Equal(new[] { 0, 1, 2, 3 }, indices);
        }

        [Fact]
        public void InsertionSelectorsAreOrderedLeftToRight() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            var offsets = CollectTags(visible).Where(t => t.IsInsertion).Select(t => t.Offset).ToList();
            for (int i = 0; i + 1 < offsets.Count; i++)
                Assert.True(offsets[i] < offsets[i + 1],
                    $"Insertion offsets must increase left to right, but {offsets[i]} >= {offsets[i + 1]}.");
        }

        [Fact]
        public void LaneSelectorsAreOrderedLeftToRight() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            var offsets = CollectTags(visible).Where(t => !t.IsInsertion).Select(t => t.Offset).ToList();
            for (int i = 0; i + 1 < offsets.Count; i++)
                Assert.True(offsets[i] < offsets[i + 1],
                    $"Lane offsets must increase left to right, but {offsets[i]} >= {offsets[i + 1]}.");
        }

        [Fact]
        public void SelectorsAreEmptyWithoutADraft() {
            var state = new RoadBuilderState();
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            Assert.Empty(CollectTags(visible));
        }

        [Fact]
        public void SelectorsTagTheSourceNodeEnd() {
            var node = MakeNode(-3f, 0f, 3f);
            var state = new RoadBuilderState();
            state.BeginFrom(node.FrontEnd);
            var renderer = MakeRenderer();
            var visible = new MultiMesh();

            renderer.AddSelectors(state, visible);

            foreach (var tag in CollectTags(visible))
                Assert.Equal(node.FrontEnd, tag.NodeEnd);
        }

        /// <summary>
        /// Pulls the distinct <see cref="LaneHit"/> tags out of a multi-mesh's render bins, in triangle
        /// order. <c>Mesh.Tags</c> is keyed by triangle index, so ordering by key gives the draw order.
        /// <para>
        /// Each quad is tagged with <c>AddTagsToLastTriangles(2, hit)</c>, so the same hit appears once per
        /// triangle; the duplicates are collapsed here.
        /// </para>
        /// </summary>
        private static List<LaneHit> CollectTags(MultiMesh mesh) {
            var result = new List<LaneHit>();
            foreach (var bin in mesh.RenderBins.Values)
                foreach (var tag in bin.Tags.OrderBy(x => x.Key).Select(x => x.Value))
                    if (tag is LaneHit hit && !result.Contains(hit)) result.Add(hit);
            return result;
        }
    }
}
