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
  **from the left**; for the `Backward` end it mirrors the index (`index = LaneCount - index - 1`) so that
  index 0 is the leftmost lane as seen looking along that end's own forward direction. `AddLane` / `Delete`
  are the mutation primitives.
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
`NodeSpecDraft`, `LaneMapping.Derive(source, dest)` produces it by a **longest common subsequence (LCS)**
match on `LaneSpec` equality, with `LaneId` carried through where the user has explicitly linked lanes.

**What LCS means here.** LCS is the standard dynamic-programming algorithm for finding the longest sequence
of elements that appears in *both* input sequences **in the same relative order**, without requiring the
elements to be contiguous. Applied to two lane lists:

- The two sequences are the source lanes and the destination lanes, each in left→right order.
- Two lanes "match" when their `LaneSpec`s are equal (`LaneSpec.Equals`, which compares `Color`,
  `VehicleTypes`, `Flags`, `SpeedLimit`, `LineWidth` and `Width`).
- The LCS is the largest set of lanes that can be paired up while preserving left→right order on both
  sides. Those pairs become `Matched`.
- Source lanes not in the LCS become `SourceOnly`; destination lanes not in the LCS become `DestOnly`.
- Each `DestOnly` lane is spliced into the source ordering next to its nearest LCS neighbour, which is
  what `Insertions` records.

The DP table is `O(n·m)` in time and space for `n` source and `m` destination lanes — trivial at road
widths (a handful of lanes), so no optimisation is needed. Order preservation is the property that makes
LCS the right tool: it will never pair the leftmost source lane with the rightmost destination lane, so
the resulting mapping is always geometrically sane and never produces crossing connectors.

Worked example — source `[car, bus, tram, bike]`, destination `[car, tram, bike, footpath]`:

```
        ""   car  tram  bike  footpath
  ""     0    0    0     0      0
  car    0    1    1     1      1
  bus    0    1    1     1      1
  tram   0    1    2     2      2
  bike   0    1    2     3      3
```

LCS = `[car, tram, bike]` → `Matched = {(car,car), (tram,tram), (bike,bike)}`,
`SourceOnly = [bus]`, `DestOnly = [footpath]`, `Insertions = [(bike, right, footpath)]`.

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
    LaneMappingDeriver.cs     // longest-common-subsequence derivation
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
| `R` | Toggle the selected lane's direction (§5.4) |
| `Shift+R` / `Ctrl+R` | Reverse all lanes / the selected lane's side |
| `E` / `Shift+E` | Exit a lane to the right / left (§5.5) |
| `Tab` | Cycle the hovered lane |
| Left-click a lane in the preview | Select it for per-lane editing |
| Right-click a lane | Context menu: duplicate, exit, delete, reverse, set spec, copy |
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

### 5.4 Toggle lane direction

Lane direction is already modelled, but only implicitly: `LaneFlags` carries `LongitudinalReverse`, and
`LaneSpec.Reverse()` (`Roads/LaneSpec.cs:73`) flips it. `LaneReconcillation.GenerateLaneConnections`
currently *guesses* direction with `LaneMappings.IsReverseLaneHeuristic` and then applies
`LongitudinalReverse` based on the build direction and the node end — the user has no direct control.

The Road Builder must expose direction as a first-class, per-lane toggle:

```csharp
// NodeSpecDraft
public void ToggleDirection(LaneId id) {
    var lane = Get(id);
    lane.Spec = lane.Spec.Reverse();   // flips LaneFlags.LongitudinalReverse
}
```

- **UI**: a direction arrow button on each lane in the cross-section strip, and a `R` shortcut for the
  selected lane. The arrow points along the segment's forward direction, or against it when reversed.
- **Visualisation**: the 3D preview draws the lane's direction arrow using `signs/forward.png` /
  `signs/backward.png` / `signs/bidirectional.png`, so a reversed lane is unmistakable before placement.
- **Bulk**: `Shift+R` reverses every lane in the draft; `Ctrl+R` reverses only the selected lane's side
  of the centreline.
- **Interaction with the heuristic**: the heuristic stays as the *initial* value when a draft is created
  from a source node, but once the user toggles a lane the draft records it as explicit and the heuristic
  no longer overrides it. This is the same "derived until touched" pattern used for `LaneMapping`.

Note that direction is a property of the **lane strip** (the connection), not of the node — a lane can
enter a node from one side and leave in the opposite direction. `LaneStrip.LaneSpec` already carries the
per-strip spec, so the toggle writes to the draft's lane spec and the strip inherits it at commit time.

### 5.5 Exit a lane

A new operation, distinct from split and from duplicate: **exit** inserts a copy of a lane *beside* it
without moving or resizing the original. This is how you build a slip road, a lay-by, a parking bay, or a
second lane that peels off — the original lane keeps its exact position and width, and the new lane is
placed immediately to its left or right.

```csharp
// NodeSpecDraft
/// <summary>
/// Inserts a copy of <paramref name="id"/> on the given side without moving the original.
/// </summary>
public LaneId Exit(LaneId id, int side, LaneSpec? overrideSpec = null) {
    var index = IndexOf(id);
    var insertAt = side < 0 ? index : index + 1;   // left of, or right of, the original
    var spec = overrideSpec ?? Get(id).Spec;       // copy by default
    return Insert(insertAt, spec);
}
```

Key properties that distinguish `Exit` from the other operations:

| Operation | Original lane | New lane | Offsets |
|---|---|---|---|
| `Insert` | unchanged | new spec | everything outside shifts outward |
| `Split` | **removed** | two lanes, widths sum to the original | original's footprint is divided |
| `Duplicate` | unchanged | copy | copy is appended at the outside |
| **`Exit`** | **unchanged, does not move** | copy, or a caller-supplied spec | only lanes *outside* the insertion point shift |

The "does not move" guarantee is what makes it an *exit* rather than an insert: the original lane's
`CenterPos` is preserved exactly, so a lane that already lines up with something on the other side of the
node stays lined up. Because offsets are derived from the ordered list (§5.1), this falls out
automatically — `Exit` is just `Insert` at `index` or `index + 1`, and the original lane's own offset is
unaffected because only the lanes beyond the insertion point are pushed.

- **UI**: `E` exits to the right, `Shift+E` exits to the left. Both are also buttons in the lane
  inspector, drawn with `signs/expand.png` (right) and a mirrored variant (left).
- **Drag gesture**: dragging a lane sideways past its neighbour's edge and releasing offers "exit here"
  as the default action, with "move" and "merge" as alternatives in a small radial menu.
- **Default spec**: the copy inherits the parent's `LaneSpec` verbatim, including direction. A common
  follow-up is to toggle the new lane's direction (§5.4) to make it a two-way pair, so the exit button
  and the direction button sit next to each other in the inspector.

### 5.6 Any amount, any position

Because the draft is an ordered list with `LaneId`s and offsets are *derived*, all of these are the same
code path:

- insert at index 0 (outside left) — today's only supported case
- insert at index `n` (outside right) — today's other case
- insert at index `k` in the middle — **new**
- remove from the middle — **new**
- reorder lanes by dragging — **new**
- widen/narrow any lane — **new**
- exit a lane beside itself without moving it (§5.5) — **new**
- toggle any lane's direction (§5.4) — **new**

---

## 6. Per-lane and per-strip configuration

### 6.1 Per-lane

Selecting a lane in the preview opens the lane inspector. `DearUI.InputLaneSpec` already exists and
already handles `VehicleTypes`, `LaneFlags`, colour, width, speed, line width and surface — it just needs
to be reachable from the 3D preview rather than only from a menu. The inspector also carries the two new
per-lane actions: the direction toggle (§5.4) and the exit button (§5.5).

### 6.2 Per-strip

A `LaneStrip` connects two lane ends. Per-strip configuration (direction, surface, markings) belongs in a second inspector that appears when a strip is selected. `ModeConnection` already has the `Reverse` / `Edit` / `Delete` actions (`Mode/ModeConnection.cs:87-113`); those should be folded into the Road Builder's strip inspector rather than living in a separate mode.

### 6.3 Per-strip road finish (elevated sections)

Elevated sections are already supported by the model: `RoadStrip` implements `IRoadFinish` and carries a
`Property<RoadFinish> FinishProperty` (`Roads/Strip/RoadStrip.cs:65-67`), defaulting to
`RoadFinish.Embankment`. `RoadFinish` (`Roads/RoadFinish.cs:37-52`) is a three-field struct:

```csharp
public struct RoadFinish {
    public Surface subsurface;   // what the deck/embankment is made of
    public float angle;          // slope angle, radians
    public float depth;          // how far it extends below the road
}
```

with four presets already defined:

| Preset | `subsurface` | `angle` | `depth` | Meaning |
|---|---|---|---|---|
| `None` | `Surface.None` | 0 | 0 | no finish geometry |
| `Embankment` | `Surface.Dirt` | π/4 | 10 | sloped earth embankment (the default) |
| `Deck` | `Surface.Concrete` | π/2 | 1 | **square concrete deck, shallow depth** — the elevated section |
| `Wall` | `Surface.Concrete` | π/2 | 10 | vertical retaining wall |

`SegmentRenderer.GenerateEndCap` (`Roads/Strip/SegmentRenderer.cs`) is what turns the finish into
geometry, and `DearUI.InputRoadFinish` already provides an editor for it, reachable today via
`Menu.ShowFinishSettings()` (`Mode/ModeSegment.cs:63`).

The Road Builder should make the finish a **per-strip property edited in the strip inspector**, not a
global tool setting:

- The strip inspector gets a finish section with the four presets as icon buttons plus the three raw
  fields (`subsurface` as a surface picker, `angle` and `depth` as drag floats).
- The finish is stored on the draft's strip, so a single segment can be embankment at one end and deck at
  the other — which is what a road climbing onto a bridge actually looks like.
- The 3D preview renders the finish through the same `GenerateEndCap` path, so the deck is visible before
  placement.
- `RoadFinish` is already serialised by `Save2/RoadFinishConverter.cs`, so no save-format change is needed.

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
| Lane direction: forward / backward / both | reuse `signs/forward.png`, `signs/backward.png`, `signs/bidirectional.png` |
| Exit lane: right / left | reuse `signs/expand.png`; add `signs/expandleft.png` (mirror) |
| Road finish: none / embankment / deck / wall | `ui/finish-none.png`, `ui/finish-embankment.png`, `ui/finish-deck.png`, `ui/finish-wall.png` |

### 7.3 Icon atlas

```csharp
// Mode/RoadBuilder/LaneIconAtlas.cs
public static class LaneIconAtlas {
    public static TextureData Vehicle(VehicleTypes t);   // single-flag lookup
    public static TextureData Surface(Surface s);
    public static TextureData Flag(LaneFlags f);
    public static TextureData Line(LineStyle s);
    public static TextureData Direction(LaneFlags f);    // forward / backward / bidirectional
    public static TextureData Finish(RoadFinish f);      // none / embankment / deck / wall
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
│  Direction: [→][←][↔]                                         │
│  Width  [ 3.5 ]  Speed [ 80 ]  Line [ 0.2 ]  Colour [ █ ]     │
│  [Duplicate] [Exit ◀] [Exit ▶] [Delete] [Merge ◀] [Merge ▶]  │
│  [Split] [Reverse] [Copy]                                     │
└──────────────────────────────────────────────────────────────┘
```

The strip inspector (§6.2, §6.3) sits below this and carries the per-strip spec plus the road finish:

```
┌──────────────────────────────────────────────────────────────┐
│  Strip:  node A → node B          Length 42.7 m              │
│  Finish:  [none][embankment][deck][wall]                      │
│  Subsurface [concrete ▾]  Angle [ 1.571 ]  Depth [ 1.0 ]      │
│  [Reverse strip] [Edit] [Delete]                              │
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
4. `LaneMapping` + `LaneMappingDeriver` (longest common subsequence on `LaneSpec`, see §2.2).
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
13. Direction toggle (§5.4): `NodeSpecDraft.ToggleDirection`, `R` shortcut, direction arrows in the
    preview, and the "explicit beats heuristic" rule.
14. Exit operation (§5.5): `NodeSpecDraft.Exit`, `E` / `Shift+E`, exit buttons in the inspector.

### Phase 4 — Clipboard and library
15. `RoadBuilderClipboard` (TextCopy + text format).
16. `RoadBuilderLibrary` with thumbnail strip/lane/finish, persisted with the `TSWorld`.

### Phase 5 — Icons
17. `LaneIconAtlas` + the missing icons from §7.2.
18. Replace the nested-menu lane editor with the icon strip editor.

### Phase 6 — Connection and finish
19. `Connecting` state, mapping visualisation, mapping editing.
20. Fold `ModeConnection`'s strip actions into the strip inspector.
21. Per-strip road finish editor (§6.3), replacing the global `Menu.ShowFinishSettings()`.

### Phase 7 — Parity and retirement
22. `ModeSegment` becomes a preset loader for the Road Builder.
23. Remove `LaneMappingInputs`, `LaneMappingOutput`, `LaneReconcillation`, `LaneCreationState`,
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
6. **Direction is a first-class per-lane toggle** (§5.4) — `LaneSpec.Reverse()` already exists; the tool
   just has to expose it instead of leaving direction to `IsReverseLaneHeuristic`.
7. **`Exit` is a new primitive** (§5.5) — insert a copy beside a lane without moving the original, which
   is what slip roads, lay-bys and parking bays need.
8. **Road finish is per-strip** (§6.3) — `RoadFinish.Deck` already gives the square concrete deck for
   elevated sections; it just needs to be editable per strip rather than globally.
6. **`HalfNodeLanesList.Insert` must actually insert** — a one-line-shaped fix with outsized consequences.
7. **Added lane specs and road finishes** to libraries - a key feature