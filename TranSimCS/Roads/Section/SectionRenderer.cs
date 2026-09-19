using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Clipper2Lib;
using NLog;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Polygons;
using TranSimCS.Render;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.SilkNet;
using TranSimCS.Spline;
using static TranSimCS.Geometry.GeometryUtils;
using static TranSimCS.Geometry.LineEnd;

namespace TranSimCS.Roads.Section {
    internal static class SectionRenderer {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static void GenerateIntersectionStrip(Mesh mesh, HalfNode start, HalfNode end, int accuracy = 17) {
            //Generate bounding edges
            var startLeft = calcBoundingLineEndFaced(start, -1);
            var startRight = calcBoundingLineEndFaced(start, 1);
            var endRight = calcBoundingLineEndFaced(end, 1);
            var endLeft = calcBoundingLineEndFaced(end, -1);

            var leftEdge = GenerateSplinePoints(startLeft.Position, endRight.Position, startLeft.Tangential, endRight.Tangential, accuracy);
            var rightEdge = GenerateSplinePoints(startRight.Position, endLeft.Position, startRight.Tangential, endLeft.Tangential, accuracy);
            var wovenStrip = WeaveStrip(leftEdge, rightEdge);

            // Apply vertical offset to prevent Z-fighting with ground
            var stripVerts = wovenStrip.Select(CreateVertex).ToArray();

            mesh.DrawStrip(stripVerts);
        }

        /// <summary>
        /// Generates a road edge from <paramref name="start"/> to <paramref name="end"/>, at left if <paramref name="discriminant"/>&lt;0, at the right otherwise
        /// </summary>
        /// <param name="start">start node</param>
        /// <param name="end">end node</param>
        /// <param name="discriminant">determines the sidea</param>
        /// <returns></returns>
        public static Bezier3 GenerateRoadEdge(HalfNode start, HalfNode end, int discriminant, Vector3 center) {
            LineEnd startPos = calcBoundingLineEndFaced(start, discriminant);
            LineEnd endPos = calcBoundingLineEndFaced(end, -discriminant);
            //Tangents must point away from the section centre: opposite nodes of a dual carriageway have
            //opposite Z axes, and an unflipped join spline dips through the section instead of wrapping around it.
            //The finish wall direction no longer depends on tangent orientation (it is radial), so this flip is safe
            var startTangent = startPos.Tangential;
            if (Vector3.Dot(startTangent, startPos.Position - center) < 0) startTangent = -startTangent;
            var endTangent = endPos.Tangential;
            if (Vector3.Dot(endTangent, endPos.Position - center) < 0) endTangent = -endTangent;
            //A join across a road mouth (chord perpendicular to both road tangents) must run straight across -
            //matching the road's own end edge - otherwise the spline bulges through the junction or across the road
            var chord = endPos.Position - startPos.Position;
            var chordLength = chord.Length();
            if (chordLength > 1e-6f) {
                var chordDir = chord / chordLength;
                if (MathF.Abs(Vector3.Dot(chordDir, startTangent.Normalized())) < 0.25f
                    && MathF.Abs(Vector3.Dot(chordDir, endTangent.Normalized())) < 0.25f) {
                    startTangent = chordDir;
                    endTangent = chordDir;
                }
            }
            return GenerateJoinSpline(startPos.Position, endPos.Position, startTangent, endTangent);
        }

        public static void GenerateSubrangeVerts(Mesh mesh, HalfNode[] nodes, int discriminant, int accuracy = 17, Vector3 center = default) {
            var lbound = 1;
            var ubound = nodes.Length - 2;

            var prevSpline = GenerateRoadEdge(nodes[0], nodes[^1], -1, center).Inverse();

            while (lbound <= ubound) {
                var preNode = nodes[lbound - 1];
                var startNode = nodes[lbound];
                var endNode = nodes[ubound];
                var nextNode = nodes[ubound + 1];
                var color = Colors.White;

                var preToStartSpline = GenerateRoadEdge(preNode, startNode, -1, center);
                var nextToEndSpline = GenerateRoadEdge(nextNode, endNode, 1, center);

                ISpline<Vector3> bottomSpline;
                var leftSpline = preToStartSpline;
                var rightSpline = nextToEndSpline;
                var topSpline = prevSpline;

                if (lbound == ubound) { //One node remaining
                    var bounds = startNode.Bounds;
                    var lpos = calcLineEnd(startNode, bounds.Min).Position;
                    var rpos = calcLineEnd(startNode, bounds.Max).Position;
                    bottomSpline = new LineSegment(rpos, lpos);
                } else { //More nodes remaining
                    var innerSpline = GenerateRoadEdge(startNode, endNode, -1, center);
                    var outerSpline = GenerateRoadEdge(startNode, endNode, 1, center);

                    //Calculations for the fill patch
                    bottomSpline = outerSpline;
                    prevSpline = innerSpline;

                    //Draw the road strip with vertical offset
                    GenerateIntersectionStrip(mesh, startNode, endNode, accuracy);
                }
                topSpline = topSpline.Inverse();

                //Render the last-node or the inter-strip patch with vertical offset
                RenderPatch.RenderCoonsPatch(mesh, bottomSpline, topSpline, leftSpline.Inverse(), rightSpline.Inverse(), (p, uv) => CreateVertex(p, color), accuracy, accuracy);

                lbound++; ubound--;
            }
        }

        private static void GenerateSectionBySlope(Mesh surfaceMesh, RoadSection roadSection, HalfNode start, HalfNode end, int accuracy = 17) {
            //Rotate the list so the 1st main end lies on the index 0
            var circularList = DLNode<HalfNode>.CreateCircular(roadSection.SortedNodes);
            var startNode = circularList;
            while (startNode.val != start) startNode = startNode.Next;
            var endNode = circularList;
            while (endNode.val != end) endNode = endNode.Next;

            //Categorize the nodes into categories: left or right of the main. Since they're already sorted, there's no need to sort.
            var rightNodes = CollectNodes(startNode, endNode);
            var leftNodes = CollectNodes(endNode, startNode);

            //Generate the main strip with vertical offset to prevent Z-fighting
            GenerateIntersectionStrip(surfaceMesh, start, end, accuracy);
            //Generate other vertices on the right
            GenerateSubrangeVerts(surfaceMesh, rightNodes.ToArray(), 1, accuracy, roadSection.Center);
            //Generate other vertices on the left
            GenerateSubrangeVerts(surfaceMesh, leftNodes.ToArray(), -1, accuracy, roadSection.Center);
        }

        private static void GenerateSectionWithoutSlope(Mesh surfaceMesh, RoadSection roadSection, int accuracy = 17) {
            var perimeter = GenerateSectionPerimeter(roadSection, accuracy)
                .Select(CreateVertex)
                .ToArray();

            if (perimeter.Length < 3) return;

            var center = CreateVertex(roadSection.Center);
            surfaceMesh.DrawCenteredPoly(center, perimeter.ToArray());
        }

        internal static void GenerateSectionSelectionMesh(RoadSection roadSection, MultiMesh multimesh) {
            if (roadSection.Nodes.Count < 1) return; //Guard agains empty sections
            var accuracy = Settings.RoadAccuracy;

            var surfaceMesh = new Mesh();
            var endsPair = roadSection.MainSlopeNodes.Value;
            var hasSlope = endsPair.Start != null
                && endsPair.End != null
                && endsPair.Start != endsPair.End
                && roadSection.Nodes.Contains(endsPair.Start)
                && roadSection.Nodes.Contains(endsPair.End);

            if (hasSlope) {
                GenerateSectionBySlope(surfaceMesh, roadSection, endsPair.Start, endsPair.End, accuracy);
            } else if (roadSection.Nodes.Count > 2) {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            } else if (roadSection.Nodes.Count == 2) {
                GenerateIntersectionStrip(surfaceMesh, roadSection.SortedNodes[0], roadSection.SortedNodes[1], accuracy);
            } else {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            }

            var renderBin = multimesh.GetOrCreateRenderBinForced(Materials.Asphalt);
            renderBin.DrawModel(surfaceMesh);
            renderBin.AddTagsToLastTriangles(-1, roadSection);
        }

        public record struct SectionTriangulationRow(Color color, PathD path, SimpleMaterial? material) {}

        internal static void GenerateSectionMesh(RoadSection roadSection, MultiMesh multimesh) {
            if (roadSection.Nodes.Count < 1) return; //Guard agains empty sections
            var accuracy = Settings.RoadAccuracy;

            var surfaceMesh = new Mesh();
            var endsPair = roadSection.MainSlopeNodes.Value;
            var hasSlope = endsPair.Start != null
                && endsPair.End != null
                && endsPair.Start != endsPair.End
                && roadSection.Nodes.Contains(endsPair.Start)
                && roadSection.Nodes.Contains(endsPair.End);

            if (hasSlope) {
                GenerateSectionBySlope(surfaceMesh, roadSection, endsPair.Start, endsPair.End, accuracy);
            } else if (roadSection.Nodes.Count > 2) {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            } else if (roadSection.Nodes.Count == 2) {
                GenerateIntersectionStrip(surfaceMesh, roadSection.SortedNodes[0], roadSection.SortedNodes[1], accuracy);
            } else {
                GenerateSectionWithoutSlope(surfaceMesh, roadSection, accuracy);
            }

            //Find qualifying road strips
            var qualifyingRoadStrips = roadSection.ContainedSegments;
            var laneStrips = qualifyingRoadStrips.SelectMany(x => x.Lanes).ToArray();

            //Group elements by type (dashed, solid, drivable, asphalt)
            var roadSplineComponents = new List<SectionTriangulationRow>? [(int)RoadSplineComponentType.Count];
            foreach (var lane in laneStrips) foreach(var stripSplineComponent in lane.AllStrips.CrossSections) {
                var projection = ProjectStripOntoWorkingPlane(roadSection, stripSplineComponent, lane.AllStrips);
                var row = roadSplineComponents[(int)stripSplineComponent.Value.Type] ??= new();
                var str = new SectionTriangulationRow(stripSplineComponent.Value.Color, projection, stripSplineComponent.Value.Texture);
                row.Add(str);
            }

            //Project each type of strip and also the boundary
            //Generate markings
            var solidLines = roadSplineComponents[(int)RoadSplineComponentType.ClippedMarking];
            var drivingLines = roadSplineComponents[(int)RoadSplineComponentType.MarkingClip];
            var asphaltLines = roadSplineComponents[(int)RoadSplineComponentType.RoadSurface];

            //Build geometry for solid lines, and apshalt
            var mergedWhites = (solidLines ?? []).Select(x => new Polygon(x.path, FillRule.EvenOdd)).AggregateOrDefault(new Polygon(), (x, y) => x | y);
            var mergedAsphalt = (asphaltLines ?? []).Select(x => new Polygon(x.path, FillRule.EvenOdd)).AggregateOrDefault(new Polygon(), (x, y) => x | y);
            var mergedDrives = (drivingLines ?? []).Select(x => new Polygon(x.path, FillRule.EvenOdd)).AggregateOrDefault(new Polygon(), (x, y) => x | y);

            //Execute gometry operations
            //Clip everything to the section perimeter: marking paths may extend past the section surface
            //(through-roads, unclipped dashes). Past the rim the projection rays would hit unrelated world geometry.
            var perimeterPath = new PathD(GenerateSectionPerimeter(roadSection, accuracy).Select(p => {
                var c = roadSection.WorkingPlane.Project(p);
                return new PointD(c.X, c.Y);
            }));
            var sectionArea = new Polygon(perimeterPath, FillRule.NonZero);
            var whiteResult = (mergedWhites - mergedDrives).Intersect(sectionArea);
            var asphaltResult = mergedAsphalt.Intersect(sectionArea);

            //Triangulate meshes
            var triangulatedWhite = PathsDTriangulation.Triangulate(whiteResult);
            var triangulatedAsphalt = PathsDTriangulation.Triangulate(asphaltResult);

            //Generate meshes for projection
            //Marking vertices are generated without elevation - the offset above the surface is re-applied
            //after the projection, otherwise it would be discarded when vertices snap onto the surface
            var projectionPlane = roadSection.WorkingPlane;
            var meshedWhite = new Mesh(null,
                triangulatedWhite.points.Select(CreateMeshingFunction(projectionPlane, Colors.White, Vector3.Zero)),
                triangulatedWhite.triangles.Select(x => (ushort)x)
            );
            var meshedAsphalt = new MultiMesh();
            var rawDashes = roadSplineComponents[(int)RoadSplineComponentType.UnclippedMarking];
            var meshedDashes = new MultiMesh();

            void ConvertProjectionToMesh(List<SectionTriangulationRow>? list, MultiMesh mesh) {
                if (list == null) return;
                foreach (var meshElement in list) {
                    if(meshElement.material == null) continue;
                    var polygon = new Polygon(meshElement.path, FillRule.EvenOdd).Intersect(sectionArea);
                    var triagulation = PathsDTriangulation.Triangulate(polygon);
                    var points = triagulation.points.Select(CreateMeshingFunction(projectionPlane, meshElement.color, Vector3.Zero));
                    var bin = mesh.GetOrCreateRenderBinForced(meshElement.material.Value);
                    bin.DrawModel(points.ToArray(), triagulation.triangles.Select(x => (ushort)x).ToArray());
                }
            }

            ConvertProjectionToMesh(rawDashes, meshedDashes);
            ConvertProjectionToMesh(asphaltLines, meshedAsphalt);

            float reach = surfaceMesh.BoundingBox().Extent();

            meshedWhite.ReverseWinding();
            meshedAsphalt.ReverseWinding();
            meshedDashes.ReverseWinding();

            //Project meshes
            var projectedWhite = meshedWhite.ProjectOnto(surfaceMesh, roadSection.Normal, float.PositiveInfinity, -reach, 0.05f);
            var projectedAsphalt = meshedAsphalt.ProjectOnto(surfaceMesh, roadSection.Normal, float.PositiveInfinity, -reach);
            var projectedDashes = meshedDashes.ProjectOnto(surfaceMesh, roadSection.Normal, float.PositiveInfinity, -reach, 0.05f);

            var asphaltMesh = multimesh.GetOrCreateRenderBinForced(Materials.Asphalt);
            var whiteMesh = multimesh.GetOrCreateRenderBinForced(Materials.EmissiveWhite);
            var dashedMesh = multimesh.GetOrCreateRenderBinForced(Materials.LineDash);
            multimesh.AddAll(projectedAsphalt);
            whiteMesh.DrawModel(projectedWhite);
            multimesh.AddAll(projectedDashes);

            GenerateSectionFinish(roadSection, multimesh, asphaltResult, surfaceMesh, accuracy);

            //Add tags to all
            multimesh.AddTagsToAll(roadSection);
        }

        private static Func<PointD, Vertex> CreateMeshingFunction(WorkingPlane projectionPlane, Color color, Vector3? offset = null, ushort material = 0, ushort emissive = 0) =>
            x => {
                var projected = x.ToVector2();
                var pos = projectionPlane.Unproject(projected) + (offset ?? Vector3.Zero);
                return new Vertex(pos, color, projected, material, emissive);
            };
        private static PathD ProjectStripOntoWorkingPlane(RoadSection roadSection, GridCrossSectionalRecord<RoadSplineComponent> component, GridMesh<Vector3, RoadSplineComponent> gridMesh) {
            var h = gridMesh.Vertices.Height();
            var points3d = new Vector3[h * 2];
            for(int i = 0; i < h; i++) {
                points3d[i] = gridMesh.Vertices[component.MinIndex, i];
                points3d[h + i] = gridMesh.Vertices[component.MaxIndex, h - i - 1];
            }
            var referencePlane = roadSection.WorkingPlane;
            PathD result = new PathD();
            foreach (var point in points3d) {
                result.Add(referencePlane.Project(point).ToPointD());
            }
            return result;
        }

        private static void GenerateSectionFinish(RoadSection roadSection, MultiMesh multimesh, Polygon asphaltPolygon, Mesh surfaceMesh, int accuracy = 17) {
            var finish = roadSection.Finish;
            var texture = finish.subsurface.GetTexture();
            if (texture == null || finish.depth <= 0) return;

            var normal = roadSection.Normal;
            if (normal.LengthSquared() < 1e-6f) normal = Vector3.UnitY;
            normal = normal.Normalized();

            var height = finish.depth;
            var breadth = finish.depth * MathF.Tan(finish.angle);

            var finishMesh = multimesh.GetOrCreateRenderBinForced(texture.Value);

            //Evaluate the outermost boundary: the outer edge of the asphalt polygon - the exact outline
            //the rendered surface ends at, so the skirt hugs it tightly. Holes (grassy islands) are
            //skipped; sections without asphalt fall back to the perimeter ring
            var plane = roadSection.WorkingPlane;
            var rings = asphaltPolygon.path.Where(p => p.Count >= 3 && Clipper.Area(p) > 0).ToArray();
            if(rings.Length == 0) {
                var perimeter = new PathD(GenerateSectionPerimeter(roadSection, accuracy).Select(p => {
                    var c = plane.Project(p);
                    return new PointD(c.X, c.Y);
                }));
                rings = [perimeter];
            }

            var reach = surfaceMesh.BoundingBox().Extent();
            foreach(var ring in rings) {
                var count = ring.Count;
                if(count > 1 && ring[0] == ring[count - 1]) count--; //drop the duplicated closing vertex
                if(count < 3) continue;

                //Ring winding decides which side of each edge is the outside (right of travel when CCW)
                double winding = 0;
                for(int j = 0; j < count; j++) {
                    var a = ring[j];
                    var b = ring[(j + 1) % count];
                    winding += a.x * b.y - b.x * a.y;
                }

                var outward = new Vector2[count];
                for(int j = 0; j < count; j++) {
                    var prev = ring[(j - 1 + count) % count];
                    var next = ring[(j + 1) % count];
                    var tangent = new Vector2((float)(next.x - prev.x), (float)(next.y - prev.y));
                    //The wall direction is the in-plane perpendicular of the tangent, signed by the ring
                    //winding so it always points away from the asphalt polygon interior
                    var perp = new Vector2(tangent.Y, -tangent.X);
                    if(perp.LengthSquared() < 1e-12f) perp = new(1, 0);
                    if(winding < 0) perp = -perp;
                    outward[j] = perp / perp.Length();
                }

                //Unproject the rim to 3D with the same function as the rest of the geometry, then snap it
                //onto the surface mesh, so the skirt follows the surface elevation exactly
                var rimMesh = new Mesh(null, ring.Take(count).Select(CreateMeshingFunction(plane, Colors.White, Vector3.Zero)));
                var snapped = rimMesh.ProjectOnto(surfaceMesh, normal, float.PositiveInfinity, -reach);
                var top = snapped.Vertices.Select(v => v.Position).ToArray();

                var bottom = new Vector3[top.Length];
                for(int j = 0; j < top.Length; j++) {
                    //Unproject the 2D direction back onto the section plane via its orthonormal axes
                    var outward3 = plane.X * outward[j].X + plane.Y * outward[j].Y;
                    bottom[j] = top[j] + outward3 * breadth - normal * height;
                }
                var generatedSplines = UniformTexturing.UniformTexturedTwin(top, bottom, UniformTexturing.GenerateLaneStripVertexGen(Colors.White));
                finishMesh.DrawStrip(generatedSplines);
            }
        }

        private static Vector3[] GenerateSectionPerimeter(RoadSection roadSection, int accuracy = 17) {
            var nodes = roadSection.SortedNodes.Rev().ToArray();
            var perimeter = new List<Vector3>();

            //The rim is the polygon through the physical road-edge endpoints: each node's bounds are the
            //endpoints of its road's edge at the junction. Visiting both ends of every node in circular order
            //keeps every mouth chord straight (matching the road's own end edge exactly) and every corner sharp.
            //Splining the corner joins made them swing wide across the approaches, ridging the section ends.
            for (int i = 0; i < nodes.Length; i++) {
                var prev = nodes[i];
                var next = nodes[(i + 1) % nodes.Length];
                var prevframe = prev.Cache.ReferenceFrame;
                var nextframe = next.Cache.ReferenceFrame;
                perimeter.Add(prevframe.O + prevframe.X * prev.Bounds.Max);
                perimeter.Add(nextframe.O + nextframe.X * next.Bounds.Min);
            }
            perimeter.Reverse();
            return perimeter.ToArray();
        }

        private static List<HalfNode> CollectNodes(DLNode<HalfNode> from, DLNode<HalfNode> to) {
            var result = new List<HalfNode>();
            var i = from;
            while (i != to) {
                result.Add(i.val);
                i = i.Next;
            }
            result.Add(i.val);
            return result;
        }
    }
}
