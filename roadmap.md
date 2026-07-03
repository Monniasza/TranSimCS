A good roadmap should keep TranSim usable throughout development. Every milestone should leave the editor in a working state while gradually replacing `RoadSection` with `DrivingArea`. I'd also aim to make each step independently useful so that progress isn't blocked by later systems.

---

# Roadmap: Driving Areas

## Phase 1 — Reference Geometry Foundation

The goal of this phase is to introduce editable geometric references without affecting the existing road rendering pipeline.

### 1.1 Reference Points

Introduce `ReferencePoint` objects.

Features:

* World position
* Optional snapping
* Shared by multiple objects
* Persistent IDs
* Standard editing (move, duplicate, delete)

Quality of life:

* Snap to terrain
* Snap to road nodes
* Snap to existing reference points
* Multi-selection support

---

### 1.2 Reference Tangents

Allow every reference point to own **zero or more tangents**.

Each tangent stores

* direction
* weight (Bezier handle length)
* optional constraints

Possible constraints:

* Free
* Mirrored
* Aligned
* Locked length

A point may therefore represent

* corner
* smooth continuation
* multi-way junction
* disconnected handles

instead of assuming exactly two handles.

Quality of life:

* Handle visualization
* Mirroring toggle
* Automatic tangent generation
* Tangent splitting

---

### 1.3 Reference Curves

Introduce

```
ReferenceCurve
```

connecting two reference points.

Initially support

* Line
* Cubic Bezier

Since a line is simply a Bezier with collapsed handles, both can share most code.

Curves should expose

* evaluation
* tangent
* length
* adaptive tessellation
* closest point
* projection

Quality of life:

* Live curve preview
* Drag handles directly
* Curve subdivision
* Curve reversal

---

# Phase 2 — Surface Editing

Roads still render exactly as today.

---

### 2.1 Surface Patches

Introduce

```
ReferenceSurface
```

implemented by

* Strip
* Coons patch
* Polygon

Every boundary references existing curves.

Rules:

* Shared endpoints only
* No T-junctions
* No gaps
* No overhangs

---

### 2.2 Automatic Closing

If a patch is almost complete,

```
A ---- B

|

C
```

automatically insert

```
B ---- C
```

using a line segment.

This removes most manual filler creation.

---

### 2.3 Automatic Hole Filling

Detect uncovered loops.

Generate temporary filler polygons using ear clipping.

Generated fillers are not editable objects.

Deleting surrounding geometry automatically regenerates them.

---

### 2.4 Surface Validation

Introduce diagnostics.

Detect

* disconnected curves
* duplicate curves
* self-intersections
* inverted patches
* overlapping patches
* non-manifold boundaries

Editor highlights invalid geometry immediately.

---

# Phase 3 — Driving Areas

Now surfaces become gameplay objects.

---

### 3.1 DrivingArea Object

Introduce

```
DrivingArea
```

A driving area owns

* reference surfaces
* reference curves
* reference points

The rendered mesh becomes an implementation detail.

---

### 3.2 Surface Generation

Generate

* pavement mesh
* BVH
* surface normals
* projection structure

This completely replaces current section mesh generation.

---

### 3.3 Surface Projection

Implement projection onto the generated surface.

Support

* nearest point
* vertical projection
* normal retrieval
* UV coordinates

Projection assumes

* continuous surface
* no overhangs

making it deterministic.

---

# Phase 4 — Road Integration

Roads become consumers of surfaces.

---

### 4.1 Segment Projection

Existing `RoadSegment`s continue to use the existing half-edge/border-spline data model, but project their generated geometry onto a `DrivingArea` when present.

If no driving area exists, behaviour remains unchanged.

---

### 4.2 Lane Projection

Project lane strips.

Maintain

* widths
* offsets
* banking
* markings

Challenges include preserving lane spacing across curved surfaces while avoiding distortion.

---

### 4.3 Node Projection

Project

* road nodes
* shoulders
* medians
* islands

onto the driving surface.

---

# Phase 5 — Editing Tools

The editor now becomes surface-aware.

---

### 5.1 Surface Sketching

Create

* points
* curves
* strips
* Coons patches

interactively.

---

### 5.2 Smart Creation

Examples:

Selecting

```
curve
curve
curve
```

offers

> Create Coons Patch

Selecting

```
closed boundary
```

offers

> Create Polygon

---

### 5.3 Surface Splitting

Support

* split curve
* split patch
* insert reference point
* merge coincident points

---

### 5.4 Surface Constraints

Support optional constraints such as

* horizontal
* fixed elevation
* tangent continuity
* coplanar patch

---

# Phase 6 — Traffic Features

Driving areas begin replacing current sections.

---

### 6.1 Entrances

Entrances become explicit objects.

Each entrance knows

* entering roads
* exiting roads
* stop line

---

### 6.2 Traffic Lights

Traffic lights attach to entrances instead of road nodes.

This naturally supports

* signal groups
* protected turns
* pedestrian crossings

---

### 6.3 Markings

Reuse the existing marking system (`LineMarking` and `FillMarking`) by projecting marking points onto the driving surface. Because marking points already support free-standing, node-attached, and relative anchors, they integrate naturally with driving areas without changing the marking data model.

---

# Phase 7 — Terrain Integration

---

### 7.1 Embankments

Generate embankments from the outer boundary of the driving area rather than individual road sections.

This avoids internal embankments showing through overlapping pavement.

---

### 7.2 Terrain Modification

Cut/fill terrain beneath the driving area.

Generate

* embankments
* retaining walls
* cliffs

only where required.

---

### 7.3 Surface Stitching

Smooth transitions between

* terrain
* roads
* adjacent driving areas

---

# Phase 8 — Optimization

Once the functionality is complete, focus on performance.

* Adaptive spline tessellation based on geometric error instead of a fixed point count.
* Adaptive patch tessellation so nearly planar areas generate very few interior vertices.
* Shared tessellation cache for curves referenced by multiple patches.
* Incremental mesh regeneration so editing one patch doesn't rebuild the entire driving area.
* Spatial acceleration structures (BVHs and projection indices) built per driving area.
* Background regeneration of large driving areas where practical.

---

## End State

At the end of this roadmap, the road system has clear responsibilities:

* **Reference Points** define editable geometric anchors with any number of tangents.
* **Reference Curves** connect points and provide reusable boundary geometry.
* **Reference Surfaces** define the explicit drivable surface using strips, Coons patches, and polygons.
* **Driving Areas** own and render those surfaces while providing projection and traffic semantics.
* **Road Segments** (using the existing half-edge and border-spline representation) generate roads and lane topology, projecting onto driving areas instead of defining the pavement themselves.
* **Road Markings**, traffic lights, and future roadside objects become independent systems that attach to the generated driving surface.

This progression keeps each milestone usable while gradually shifting TranSim from an inferred, node-based intersection model to an explicit surface model that should scale much better to highway interchanges, plazas, parking areas, and other complex paved spaces.
