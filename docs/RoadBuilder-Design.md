# TranSim Road Builder — Design Proposal

A redesign of the road creation tool (`TranSimCS.Mode.ModeSegment`) into a **Road Builder**:
a node-spec-centric editor in the spirit of the Cities: Skylines *Road Builder* mod, but built on
TranSim's own flexible lane model rather than on predefined segment templates.

---

## 1. Where the current tool stands

### 1.1 What exists today

| Concern | Current implementation |
|---|---|
| Tool | `TranSimCS/Mode/ModeSegment.cs` — one `IMode` |
| Build model | "Build from the previous segment": pick a source `RoadNodeEnd`, drag to a destination, the destination node's lanes are derived from the source |
| Lane edits | Merge/expand the **outermost** motor lanes only; exclude lanes from the **start** node only |
| Lane mapping | `Mode/RoadConstruction/LaneMappingInputs.cs`, `LaneMapping.cs`, `LaneMappingsGenerator.cs`, `LaneReconcillation.cs` |
| Lane creation | `LaneCreationState.cs` + `Select/AddLaneSelection.cs` — four "+" quads, one per (side, end) |
| Presets | `RoadPresets.cs` — a fixed list of whole-road presets |
| UI | `SilkNet/DearUI.cs` (`InputLaneSpec`, `MenuToggle`, …), tool windows in `SilkNet/SilkNetTest.ToolWindows.cs` |
| Icons | `TranSimCS/Files/textures/ui/*.png` (40 files), `signs/*.png`, `lines/*.png`, `markings/*.png` |

### 1.2 The data model (this is the good part)

The model is already far more flexible than the tool that drives it:

- **`RoadNode`** (`Roads/Node/RoadNode.cs`) owns a flat set of `Lane`s (`Lanes`, `LaneXRef`), each with a
  `LaneNode` definition (`LaneSpec` + `CenterPos`). `SortedLanes` orders them by `CenterPos`.
- **`HalfNode`** (`Roads/Node/HalfNode.cs`) is one end of a node. `GetLaneByIndex(i)` returns the i-th lane
  **from the left**, mirrored for the `Backward` end. `AddLane` / `Delete` are the mutation primitives.
- **`LaneSpec`** (`Roads/LaneSpec.cs`) is a value struct: `Color`, `VehicleTypes`, `LaneFlags`, `Width`,
  `SpeedLimit`, `LineWidth`, `Surface`. `VehicleTypes` is a `[Flags]` enum covering car/truck/bus/bike/ped/horse/
  LRT/train/plane/rocket — i.e. **the "rail-tram-pedestrian-bike-car-pier-canal road" is already expressible**.
- **`NodeSpec`** (`Roads/Node/NodeSpec.cs`) is the serialisable description of a node's lanes.
- **`RoadStrip` / `LaneStrip`** (`Roads/Strip/`) connect lane ends across a segment.

**Conclusion:** the bottleneck is not the model, it is the *authoring surface*. The tool only ever
exposes "outermost lane, one at a time". Everything below is about fixing that.

### 1.3 Why inside-merging is hard today

`LaneMappingInputs` / `LaneMappingOutput` were an attempt to generalise this. The reason they are not
used is structural, not incidental:

- `LaneMappingInputs` describes a mapping as **`(source lane index, destination lane index)` pairs**.
  That is a *positional* description. It cannot express "insert a lane between index 2 and 3", because
  inserting shifts every index after it — the mapping is invalidated by its own application.
- `LaneReconcillation` then has to *re-derive* the destination lane list from the source list plus the
  mapping, which is exactly the "merge/expand the outermost lane" special case generalised by one step.
- `AddLaneSelection` (`Select/AddLaneSelection.cs`) is a **struct with a `side` and a `position`**, and
  `HalfNodeLanesList.Insert(index, item)` is implemented as `Add(item)` — insertion is not supported at
  all. So even the "+" buttons can only append at the outside.

**The fix is to stop describing lane edits positionally and start describing them as an ordered list of
lane *identities*.** That single change is what unlocks "merge and expand any lane, in any amount, by any
amount".

---

## 2. Core idea: the node spec is the document

Everything in the Road Builder is a view onto one value:

```
NodeSpec  =  ordered list of LaneSpec, ordered left → right, with a centreline
```

- A **segment** is `(NodeSpec start, NodeSpec end, alignment)`.
- A **node** is a `NodeSpec` plus the strips that connect it to its neighbours.
- A **preset** is a saved `NodeSpec`.
- The **clipboard** holds a `NodeSpec` (or a slice of one).
- The **node library** is a list of named `NodeSpec`s.
- The **lane library** is a list of named `LaneSpec`s.
- The **finish library** is a list of named `RoadFinish`es.

The tool never mutates the world directly. It mutates a **draft `NodeSpec`**, renders it, and commits on
click. This is what gives "modify before placement" and "instant feedback" for free.

### 2.1 Lane identity, not lane index

Introduce a stable identity for a lane *within a draft*:

```csharp
// Mode/RoadBuilder/LaneId.cs
public readonly record struct LaneId(int Value);   // unique within one NodeSpecDraft
```

A draft is:

```csharp
// Mode/RoadBuilder/NodeSpecDraft.cs
public sealed class NodeSpecDraft {
    public List<LaneDraft> Lanes;      // ordered left → right, never reordered implicitly
    public float Centerline;           // offset of the centreline, for asymmetric roads
    public float? ForcedWidth;         // null = derive from lane widths

    public LaneId Insert(int index, LaneSpec spec);   // index 0..Lanes.Count
    public void Remove(LaneId id);
    public void Move(LaneId id, int newIndex);
    public void SetSpec(LaneId id, LaneSpec spec);
    public NodeSpecDraft Clone();
    public NodeSpec ToNodeSpec();
    public static NodeSpecDraft FromNodeSpec(NodeSpec spec);
}
```

Because lanes carry `LaneId`, an edit is `(LaneId, operation)` — **insertion never invalidates anything**.
This is the direct replacement for `LaneMappingInputs`.

### 2.2 What replaces `LaneMappingInputs` / `LaneMappingOutput`

Keep the *idea* (a mapping between a source node's lanes and a destination node's lanes) but change the
representation from index pairs to **identity pairs plus an explicit insertion list**:

```csharp
// Mode/RoadBuilder/LaneMapping.cs  (replaces LaneMappingInputs + LaneMappingOutput)
public sealed class LaneMapping {
    // Lanes that exist on both sides, matched by identity.
    public List<(LaneId Source, LaneId Dest)> Matched;
    // Lanes present only on the source (they terminate at the node).
    public List<LaneId> SourceOnly;
    // Lanes present only on the destination (they begin at the node).
    public List<LaneId> DestOnly;
    // Where DestOnly lanes are spliced into the source ordering.
    public List<(LaneId Anchor, int Side, LaneId Inserted)> Insertions;
}
```

`LaneMapping` is now **derivable** rather than authored: given a source `NodeSpecDraft` and a destination
`NodeSpecDraft`, `LaneMapping.Derive(source, dest)` produces it by a longest-common-subsequence match on
`LaneSpec` equality, with `LaneId` carried through where the user has explicitly linked lanes.

This is the piece that makes "connect from and to any node" work: the mapping is computed, shown to the
user as coloured connectors, and editable — but it is never the *source of truth* for the geometry.

---

## 3. The Road Builder tool

### 3.1 New mode

```
Mode/RoadBuilder/
    ModeRoadBuilder.cs        // IMode implementation
    NodeSpecDraft.cs          // the editable document
    LaneId.cs
    LaneMapping.cs            // replaces LaneMappingInputs/LaneMappingOutput
    LaneMappingDeriver.cs     // LCS-based derivation
    RoadBuilderState.cs       // tool state machine
    RoadBuilderClipboard.cs   // copy/paste, TextCopy-backed
    RoadBuilderLibrary.cs     // named saved node specs/lane specs/road finishes
    RoadBuilderRenderer.cs    // 3D preview + drag ghosts
    RoadBuilderUI.cs          // ImGui panels
    LaneIconAtlas.cs          // icon lookup for the lane spec editor
```

`ModeSegment` stays as-is until the new tool reaches parity, then becomes a thin wrapper that opens the
Road Builder with a preset loaded. Do not delete it in the first pass.

### 3.2 State machine

```
Idle
 └─ pick source node end ──────────────► SourcePicked
      └─ drag ─────────────────────────► Dragging      (live preview, spec editable)
           └─ release on empty ────────► Placing     (commit on click)
           └─ release on node ─────────► Connecting  (mapping preview, commit on click)
                └─ Esc / right-click ──► back one step
```

`RoadBuilderState` holds:

```csharp
public sealed class RoadBuilderState {
    public RoadNodeEnd? SourceEnd;
    public NodeSpecDraft Draft;            // the spec being built
    public NodeSpecDraft? SourceDraft;     // snapshot of the source node's spec
    public LaneMapping? Mapping;           // derived, shown while Connecting
    public LaneId? HoveredLane;
    public LaneId? DraggedLane;
    public int DropIndex;                  // insertion point while dragging
    public bool Mirror;                    // mirror the draft left↔right
    public bool ApplyToBothEnds;
}
```

### 3.3 Interaction model

| Action | Result |
|---|---|
| Left-click a node end | Pick it as the source; `SourceDraft` snapshotted |
| Drag from source | Live segment preview; the draft is the source spec by default |
| Scroll while dragging | Change the number of lanes (add/remove at the hovered position) |
| `[` / `]` | Shift the centreline left/right |
| `M` | Mirror the draft |
| `Tab` | Cycle the hovered lane |
| Left-click a lane in the preview | Select it for per-lane editing |
| Right-click a lane | Context menu: duplicate, delete, reverse, set spec, copy |
| `Ctrl+C` / `Ctrl+V` | Copy/paste the selected lane(s) or the whole spec |
| `Ctrl+Shift+V` | Paste as a new preset into the library |
| `Esc` | Cancel the current step |
| `Enter` | Commit |

**Instant feedback** comes from the draft being rendered every frame by `RoadBuilderRenderer`, using the
same `NodeRenderer.GenerateLaneQuad` / `GenerateLaneMesh` path the real nodes use — so the preview is
pixel-identical to the placed result.

---

## 4. Drag and drop from clipboard and library

### 4.1 Clipboard

`TextCopy` is already a dependency (`TranSimCS.csproj`). Serialise a `NodeSpec` (or a lane slice) to a
compact text form so it can be pasted into a text editor, a chat message, or another TranSim instance:

```
TRANSSIM-NODESPEC v1
centerline=0.0
lane car,truck w=3.5 speed=50 line=0.2 surface=asphalt color=#3C3C3C
lane bus w=3.5 speed=50 flags=stop
lane lrt w=3.5 speed=80 surface=rail
lane pedestrian w=2.0 surface=pavement
```

`RoadBuilderClipboard` wraps `TextCopy.ClipboardService` with a fallback to an in-process buffer when the
OS clipboard is unavailable (headless / CI).

### 4.2 Library

`RoadBuilderLibrary` is a named list of `NodeSpec`s, `LaneSpec`s and `RoadFinish`es persisted next to the world file. The UI is a horizontal strip of **lane-stack thumbnails** — each entry renders a miniature cross-section using the same lane colours, so a "tram + 2 car + bike + footpath" preset is recognisable at a glance.
Drag from the library strip onto the 3D preview to insert that spec's lanes at the drop position.
Drag from the preview back onto the library strip to save.

### 4.3 Drag-and-drop mechanics

The existing selection pipeline already gives us everything needed:

- `Mode.AddSelectors(invisible, visible)` (`Mode/IMode.cs:72`) lets the tool publish its own pickable
  geometry each frame.
- `SilkNetTest.Rendering.cs:81` merges those into `World.TempSelectorsMesh`, and
  `SilkNetTest.Inputs.cs:31` resolves `MouseOver` from `World.RootIndex` against the mouse ray.
- `Mesh.AddTagsToLastTriangles(count, tag)` (`Model/RenderUtil.cs:170`) attaches an arbitrary object to
  the triangles just drawn, and `MeshUtil.RayIntersectMesh` returns it as `Selection.Tag`.

So: draw each lane's quad, tag it with a `LaneHit { LaneId Id; int Index; float Offset; }`, and the
existing picker hands back exactly which lane the cursor is over — **including the exact offset along the
cross-section**, which is what makes "insert between lane 2 and 3" unambiguous.

`AddLaneSelection` (`Select/AddLaneSelection.cs`) should be generalised into:

```csharp
public struct LaneHit : IRoadElement {
    public LaneId Id;
    public int Index;          // 0 = leftmost
    public float Offset;       // cross-section coordinate of the hit
    public RoadNodeEnd NodeEnd;
    public bool IsInsertion;   // true when hovering the gap between two lanes
}
```

and `HalfNodeLanesList.Insert` (`Roads/Node/HalfNodeMethods.cs:40`) must be implemented for real —
currently it is `Add(item)`, which is why inside-insertion is impossible today.

---

## 5. Merge and expand any lane, in any amount, by any amount

This is the requirement that drives the whole design. Three distinct operations:

### 5.1 Expand (widen)

`LaneSpec.Width` is per-lane and already respected by the renderer. Widening a lane in the middle of a
cross-section must **push** the lanes outside it outward, not overlap them. So the draft stores lanes as
an ordered list and derives offsets:

```csharp
// NodeSpecDraft
public float[] ComputeOffsets() {
    var widths = Lanes.Select(l => l.Spec.Width).ToArray();
    var total  = widths.Sum();
    var x = Centerline - total / 2f;
    var result = new float[Lanes.Count];
    for (int i = 0; i < Lanes.Count; i++) { result[i] = x + widths[i] / 2f; x += widths[i]; }
    return result;
}
```

`Centerline` is what makes asymmetric roads (e.g. 3 lanes one way, 1 the other) expressible without
special-casing. `NodeSpec.Range` is then `[min - w/2, max + w/2]`.

### 5.2 Merge (combine two lanes into one)

Merging lane *i* and *i+1* means: remove both, insert one whose `LaneSpec` is the **union** of the two:

```csharp
public static LaneSpec Merge(LaneSpec a, LaneSpec b) => new(
    Color:        a.Color,                       // keep the left lane's colour
    VehicleTypes: a.VehicleTypes | b.VehicleTypes,
    Width:        MathF.Max(a.Width, b.Width),   // or a.Width + b.Width for a "wide" lane
    SpeedLimit:   MathF.Min(a.SpeedLimit, b.SpeedLimit),
    Flags:        a.Flags | b.Flags,
    LineWidth:    a.LineWidth,
    Surface:      a.Surface
);
```

Because `VehicleTypes` is a flags enum, merging a car lane with a tram lane yields a lane that carries
both — which is exactly the "any road combination" the brief asks for. The merge is offered as a
**drag gesture**: drag lane *i* onto lane *i+1* and release; the ghost shows the union spec before commit.

### 5.3 Split

The inverse: split lane *i* at the cursor offset into two lanes, each inheriting the parent spec, with
widths proportional to the split point. This is what makes "expand by any amount" work for a lane that
carries two vehicle types.

### 5.4 Any amount, any position

Because the draft is an ordered list with `LaneId`s and offsets are *derived*, all of these are the same
code path:

- insert at index 0 (outside left) — today's only supported case
- insert at index `n` (outside right) — today's other case
- insert at index `k` in the middle — **new**
- remove from the middle — **new**
- reorder lanes by dragging — **new**
- widen/narrow any lane — **new**

---

## 6. Per-lane and per-strip configuration

### 6.1 Per-lane

Selecting a lane in the preview opens the lane inspector. `DearUI.InputLaneSpec` already exists and
already handles `VehicleTypes`, `LaneFlags`, colour, width, speed, line width and surface — it just needs
to be reachable from the 3D preview rather than only from a menu.

### 6.2 Per-strip

A `LaneStrip` connects two lane ends. Per-strip configuration (direction, surface, markings) belongs in a second inspector that appears when a strip is selected. `ModeConnection` already has the `Reverse` / `Edit` / `Delete` actions (`Mode/ModeConnection.cs:87-113`); those should be folded into the Road Builder's strip inspector rather than living in a separate mode.

---

## 7. Revamped lane spec editor using `Files/textures` icons

### 7.1 What is available

`TranSimCS/Files/textures/` contains (relevant subsets):

- `ui/` — `car.png`, `truck.png`, `bus.png`, `train.png`, `addlane.png`, `crosswalk.png`, `junction.png`,
  `node.png`, `road.png`, `line.png`, `points.png`, `snap.png`, `anarchy.png`, `alignl/alignc/alignr.png`,
  `curved.png`, `sbend.png`, `flatIncline.png`, `flatTilt.png`, `sectionweld.png`, `toolRoad.png`,
  `toolSpline.png`, `toolSplit.png`, `settings.png`, `customsettings.png`, `preceditor.png`, `graphedit.png`,
  `check.png`, `panel.png`, `outline.png`, `vk.png`, `ylocal.png`, `chain.png`, `blast.png`, `blast2.png`
- `signs/` — `forward.png`, `backward.png`, `bidirectional.png`, `merge.png`, `mergeleft.png`,
  `mergeright.png`, `expand.png`, `noleft.png`, `noright.png`, `parking.png`, `priority.png`, `stop.png`,
  `yield.png`, `trafficbarrier.png`, `relationreverse.png`
- `lines/` — `solid.png`, `dashed.png`, `semidashed.png`, `alternating.png`, `yield.png`
- `markings/` — `arrow.png`

### 7.2 Gaps to fill

`DearUI.cs:126-135` already references `ui/car`, `ui/truck`, `ui/bus`, `ui/train` and `signs/stop`, but
several vehicle types fall back to `ui/check` (bicycle, pedestrian, horse, plane, rocket). New icons are
needed for:

| Missing | Suggested file |
|---|---|
| Bicycle | `ui/bike.png` |
| Pedestrian | `ui/pedestrian.png` |
| Horse | `ui/horse.png` |
| Plane | `ui/plane.png` |
| Rocket | `ui/rocket.png` |
| LRT (distinct from train) | `ui/lrt.png` |
| Surface: asphalt / pavement / rail / gravel | `ui/surface-asphalt.png`, `ui/surface-pavement.png`, `ui/surface-rail.png`, `ui/surface-gravel.png` |
| Lane flags: parking, crosswalk, stop, yield, priority | reuse `signs/parking.png`, `ui/crosswalk.png`, `signs/stop.png`, `signs/yield.png`, `signs/priority.png` |

### 7.3 Icon atlas

```csharp
// Mode/RoadBuilder/LaneIconAtlas.cs
public static class LaneIconAtlas {
    public static TextureData Vehicle(VehicleTypes t);   // single-flag lookup
    public static TextureData Surface(Surface s);
    public static TextureData Flag(LaneFlags f);
    public static TextureData Line(LineStyle s);
}
```

Textures load through the existing `TexturePipeline.GetTexture(name)` (`SilkNet/TexturePipeline.cs`),
which already caches by absolute path and resolves relative to `Files/textures`. For ImGui display,
`RenderManager.GetCachedTexture(TextureData)` (`SilkNet/RenderManager.cs:77`) gives a `TextureGPU` whose
`GetHandle()` is the `ImTextureID`.

### 7.4 Editor layout

Replace the current nested-menu `InputLaneSpec` with a **cross-section strip editor**:

```
┌──────────────────────────────────────────────────────────────┐
│  [◀]  ┌────┬────┬────┬────┬────┐  [▶]                        │
│       │ 🚗 │ 🚌 │ 🚊 │ 🚲 │ 🚶 │   ← click a lane to select  │
│       │3.5 │3.5 │3.5 │2.0 │2.0 │   ← drag edges to resize    │
│       └────┴────┴────┴────┴────┘                             │
│  ─────────────────────────────────────────────────────────   │
│  Selected lane:  🚊 Light rail                                │
│  Vehicles:  [🚗][🚚][🚌][🚲][🚶][🐴][🚊][🚆][✈][🚀]          │
│  Flags:     [🅿][🚦][🛑][⚠][⬆]                                │
│  Surface:   [asphalt][pavement][rail][gravel]                 │
│  Width  [ 3.5 ]  Speed [ 80 ]  Line [ 0.2 ]  Colour [ █ ]     │
│  [Duplicate] [Delete] [Merge ◀] [Merge ▶] [Split] [Copy]     │
└──────────────────────────────────────────────────────────────┘
```

Every toggle is an icon button with a tooltip; the text labels remain for accessibility. The strip is
the primary editing surface — clicking a lane in the strip and clicking it in the 3D preview are the same
selection.

---

## 8. Accurate visualisation

The preview must be indistinguishable from the placed result. Concretely:

1. **Reuse the real renderers.** `NodeRenderer.GenerateLaneMesh` (`Roads/Node/NodeRenderer.cs:82`) and
   `StripRenderer` already draw markings, stop lines, surfaces and lane colours. The preview should call
   them against a temporary `RoadNode` built from the draft, not a bespoke preview renderer.
2. **Build a throwaway node.** `NodeSpecDraft.ToNodeSpec()` → construct a detached `RoadNode` at the
   cursor position, render it, discard it. This guarantees the preview cannot drift from reality.
3. **Ghost the delta.** While dragging, render the *current* state at full opacity and the *proposed*
   state at ~50% alpha, so the user sees exactly what changes.
4. **Show the cross-section.** A small 2D cross-section widget in the corner mirrors the 3D preview and
   is the drag target for lane reordering — this is the single biggest usability win over the current tool.
5. **Show the mapping.** While `Connecting`, draw coloured connectors between source lane ends and
   destination lane ends, using the `LaneMapping` (matched = green, source-only = red, dest-only = blue,
   insertion = yellow). This makes "connect from and to any node" legible.

---

## 9. Copy and paste node specs

```csharp
// Mode/RoadBuilder/RoadBuilderClipboard.cs
public static class RoadBuilderClipboard {
    public static void Copy(NodeSpec spec);
    public static void CopyLanes(IEnumerable<LaneSpec> lanes);
    public static NodeSpec? Paste();
    public static bool HasSpec { get; }
}
```

Serialisation is the text format from §4.1, so a spec can be shared outside the game. Round-trip must be
lossless for `LaneSpec` (all seven fields) and for lane ordering.

Paste targets:
- onto the 3D preview → replaces the draft
- onto the cross-section strip → inserts at the drop index
- onto the library strip → adds a named entry

---

## 10. Connect from and to any node

Today the tool builds *from* a source node end. The Road Builder should support all four combinations:

| From | To | Behaviour |
|---|---|---|
| node end | empty space | create a new node with the draft spec |
| node end | existing node end | derive a `LaneMapping`, show it, commit on click |
| empty space | node end | same, reversed |
| empty space | empty space | create both nodes |

The `LaneMapping` derivation (§2.2) is what makes the middle two cases work for *arbitrary* specs. When
the source and destination specs differ, the user is shown the mapping and can:
- accept it (lanes that exist on both sides are connected; others terminate/begin),
- edit it by dragging connectors,
- or force a full replacement (destination spec := source spec).

---

## 11. Implementation plan

Ordered so that each step is independently testable and the old tool keeps working throughout.

### Phase 0 — Foundations (no user-visible change)
1. `LaneId`, `NodeSpecDraft` with `Insert`/`Remove`/`Move`/`SetSpec`/`ComputeOffsets`/`ToNodeSpec`.
2. Implement `HalfNodeLanesList.Insert` properly (`Roads/Node/HalfNodeMethods.cs:40`).
3. Unit tests: round-trip `NodeSpec ↔ NodeSpecDraft`, offset computation for asymmetric specs,
   insert/remove at every index.

### Phase 1 — Mapping
4. `LaneMapping` + `LaneMappingDeriver` (LCS on `LaneSpec`). <!-- Unclear symbol: LCS --->
5. Tests: identical specs → all matched; disjoint specs → all source-only/dest-only; partial overlap.
6. Retire `LaneMappingInputs` / `LaneMappingOutput` once `LaneReconcillation` is ported.

### Phase 2 — The tool skeleton
7. `ModeRoadBuilder` implementing `IMode`, registered in `AvailableModes` (`SilkNet/SilkNetTest.cs`).
8. `RoadBuilderState` state machine, `RoadBuilderRenderer` building a throwaway `RoadNode` from the draft.
9. `LaneHit` selector published via `AddSelectors`, replacing `AddLaneSelection` for this tool.

### Phase 3 — Editing
10. Cross-section strip widget (`RoadBuilderUI`), lane selection, resize by dragging edges.
11. Insert / remove / reorder / merge / split, all via `LaneId`.
12. Per-lane inspector wired to `DearUI.InputLaneSpec`.

### Phase 4 — Clipboard and library
13. `RoadBuilderClipboard` (TextCopy + text format).
14. `RoadBuilderLibrary` with thumbnail strip/lane/finish, persisted with the `TSWorld`.

### Phase 5 — Icons
15. `LaneIconAtlas` + the missing icons from §7.2.
16. Replace the nested-menu lane editor with the icon strip editor.

### Phase 6 — Connection
17. `Connecting` state, mapping visualisation, mapping editing.
18. Fold `ModeConnection`'s strip actions into the strip inspector.

### Phase 7 — Parity and retirement
19. `ModeSegment` becomes a preset loader for the Road Builder.
20. Remove `LaneMappingInputs`, `LaneMappingOutput`, `LaneReconcillation`, `LaneCreationState`,
    `AddLaneSelection` once nothing references them.

---

## 12. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Preview drifts from placed geometry | Preview renders a real `RoadNode` built from the draft, using the production renderers |
| `NodeSpec` serialisation is lossy | Round-trip tests over every `LaneSpec` field and lane ordering |
| Performance: rebuilding a `RoadNode` every frame | Cache the throwaway node; rebuild only when the draft's `GeometryVersion` changes |
| `HalfNode.GetLaneByIndex` mirrors for `Backward` ends | All draft indices are in **left→right world order**; mirroring happens only at the `HalfNode` boundary |
| Existing worlds break | `NodeSpec` on-disk format is unchanged; the draft is a tool-side concept only |
| Scope creep into `ModeSegment` | `ModeSegment` is frozen until Phase 7 |

---

## 13. Summary of the key changes

1. **`NodeSpecDraft` with `LaneId` identity** replaces positional lane mapping — this is what makes
   inside-insertion, reordering and arbitrary merging possible.
2. **`LaneMapping` derived, not authored** — replaces `LaneMappingInputs`/`LaneMappingOutput` and makes
   "connect any node to any node" tractable.
3. **The draft is the document** — the tool edits a value and commits it, giving "modify before placement"
   and "instant feedback" without special-casing.
4. **Preview = a real `RoadNode`** — guarantees accurate visualisation.
5. **Cross-section strip editor with icons** — replaces the nested-menu lane editor and is the primary
   surface for per-lane and per-strip configuration.
6. **`HalfNodeLanesList.Insert` must actually insert** — a one-line-shaped fix with outsized consequences.
7. **Added lane specs and road finishes** to libraries - a key feature