# Road Node Editor

The Road Node Editor is an advanced road node editor that allows creation of any type of a road node, with lanes in any order, and copying the lanes
The Road Node Editor is a planned replacement for the Node Creation Tool, that additionally support arbitrary node editing, which is handy for editing road nodes

## Why?

I've attempted to code a different tool (Road Builder), but it got bloated and expensive quickly. It cost so much, it needed 8 separate development phases to complete.

### Editor layout

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