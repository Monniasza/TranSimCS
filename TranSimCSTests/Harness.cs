using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Text;
using TranSimCS;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Save2;
using TranSimCS.Collections;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;
using TranSimCS.SilkNet;
using TranSimCS.Terrain;
using TranSimCS.Cars;
using TranSimCS.Worlds;

public static class Harness {
    private static void Main() {
        var culture = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.CurrentCulture.Clone();
        culture.NumberFormat.NumberDecimalSeparator = ".";
        System.Globalization.CultureInfo.CurrentCulture = culture;

        var gameBin = @"C:\Users\Oskar\SynologyDrive\Programowanie\TranSimCS\TranSimCS\bin\Debug";
        typeof(Program).GetProperty("DataRoot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, gameBin);
        JsonProcessor.Init();
        Materials.ReadAssets();
        Car.Init();

        var world = TSWorld.LoadFromFile(@"C:\Users\Oskar\AppData\Roaming\TranSim\saves\interchanges with traffic lights 4.transim");
        Console.WriteLine($"nodes={world.Nodes.data.Count} sections={world.RoadSections.data.Count} roads={world.RoadSegments.data.Count}");

        var svg = new StringBuilder();
        svg.AppendLine(@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""1200"" height=""1200"" viewBox=""-60 -60 120 120"">");
        svg.AppendLine(@"<rect x=""-60"" y=""-60"" width=""120"" height=""120"" fill=""#4a7a3a""/>");

        //Road strips near the junction: blue-ish
        foreach (var road in world.RoadSegments.data) {
            foreach (var lane in road.Lanes) {
                MultiMesh mm;
                try { mm = lane.GetMesh(); } catch { continue; }
                if (!NearOrigin(mm)) continue;
                AppendMesh(svg, mm, "#9099c0", 0.55f);
            }
        }

        //Section 1 on top: asphalt dark, white bright, dash yellow
        var sectionIt = world.RoadSections.data.GetEnumerator();
        sectionIt.MoveNext();
        var section = (RoadSection)sectionIt.Current;
        Console.WriteLine($"section 1 center={section.Center}");
        MultiMesh smm;
        try { smm = section.Mesh.GetMesh(); } catch (Exception ex) { Console.WriteLine($"section 1 EXCEPTION: {ex.Message}"); return; }
        foreach (var bin in smm.RenderBins) {
            var isDash = bin.Key.Equals(Materials.LineDash);
            var isWhite = bin.Key.Equals(Materials.EmissiveWhite);
            var color = isDash ? "#ffe680" : isWhite ? "#ffffff" : "#2a2a2a";
            AppendMesh(svg, bin.Value, color, 1f);
        }

        //All sections: stretched triangles and big lateral vertex moves
        int si = 0;
        var sectionIt2 = world.RoadSections.data.GetEnumerator();
        while (sectionIt2.MoveNext()) {
            si++;
            var sec = (RoadSection)sectionIt2.Current;
            MultiMesh mm;
            try { mm = sec.Mesh.GetMesh(); } catch { continue; }
            foreach (var bin in mm.RenderBins) {
                var idx = bin.Value.Indices;
                var vs = bin.Value.Vertices;
                int stretched = 0;
                for (int t = 0; t <= idx.Count - 3; t += 3) {
                    var a = vs[idx[t]].Position;
                    var b = vs[idx[t + 1]].Position;
                    var c = vs[idx[t + 2]].Position;
                    var spread = MathF.Max(MathF.Abs(a.Y - b.Y), MathF.Max(MathF.Abs(b.Y - c.Y), MathF.Abs(a.Y - c.Y)));
                    var xzSpread = MathF.Max(MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z)), MathF.Max(MathF.Sqrt((b.X - c.X) * (b.X - c.X) + (b.Z - c.Z) * (b.Z - c.Z)), MathF.Sqrt((a.X - c.X) * (a.X - c.X) + (a.Z - c.Z) * (a.Z - c.Z))));
                    if (spread > 0.5f && xzSpread < spread) {
                        stretched++;
                        if (stretched <= 2) Console.WriteLine($"section {si} STRETCHED [{bin.Key.Texture}]: A=({a.X:F2},{a.Y:F2},{a.Z:F2}) B=({b.X:F2},{b.Y:F2},{b.Z:F2}) C=({c.X:F2},{c.Y:F2},{c.Z:F2})");
                    }
                }
                if (stretched > 0) Console.WriteLine($"section {si} bin[{bin.Key.Texture}]: {stretched} stretched triangles");
            }
        }

        //Grid sweep: compare section-draped surface height vs road lane surface height per cell
        var roadTris = new List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc)>();
        foreach (var road in world.RoadSegments.data) {
            foreach (var lane in road.Lanes) {
                MultiMesh mm;
                try { mm = lane.GetMesh(); } catch { continue; }
                foreach (var bin in mm.RenderBins)
                    CollectXZTris(bin.Value, roadTris);
            }
        }
        var sectionTris = new List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc)>();
        foreach (var bin in smm.RenderBins)
            CollectXZTris(bin.Value, sectionTris);

        int hover = 0, fight = 0;
        for (float gx = -30; gx <= 30; gx += 0.5f) {
            for (float gz = -30; gz <= 30; gz += 0.5f) {
                var p = new Vector2(gx, gz);
                bool secHas = SampleHeight(sectionTris, p, out var secY);
                bool roadHas = SampleHeight(roadTris, p, out var roadY);
                if (secHas && roadHas) {
                    var d = secY - roadY;
                    if (d > 0.05f) { hover++; if (hover < 15) Console.WriteLine($"HOVER at ({gx:F1},{gz:F1}): section {secY:F2} vs road {roadY:F2} (d={d:F2})"); }
                    else if (MathF.Abs(d) <= 0.05f) fight++;
                }
            }
        }
        Console.WriteLine($"SWEEP: hover={hover} fight={fight} (cells 0.5m over [-30..30]^2, section vs road top surfaces)");

        //Rebuild the section surface fan and probe its coverage directly
        var rendererType = section.GetType().Assembly.GetType("TranSimCS.Roads.Section.SectionRenderer")!;
        var perimeterMethod = rendererType.GetMethod("GenerateSectionPerimeter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var perimeterWorld = (Vector3[])perimeterMethod!.Invoke(null, new object[] { section, 17 })!;
        var fan = new Mesh();
        fan.DrawCenteredPoly(GeometryUtils.CreateVertex(section.Center), perimeterWorld.Select(GeometryUtils.CreateVertex).ToArray());
        var fanTris = new List<(Vector2, Vector2, Vector2, float, float, float)>();
        CollectXZTris(fan, fanTris);
        Console.WriteLine($"section nodes: {string.Join(" ", section.Nodes.Select(n => $"{n.PositionData.Position}"))}");
        Console.WriteLine("perimeter:");
        foreach (var p in perimeterWorld)
            Console.WriteLine($"  ({p.X:F2},{p.Y:F2},{p.Z:F2})");
        foreach (var (px, pz) in new[] { (0f, 10f), (0f, 8f), (0f, 5f), (0f, 11f), (0f, 11.9f) }) {
            bool covered = SampleHeight(fanTris, new(px, pz), out var fy);
            //two-ray probe like ProjectOnto
            var origin = new Vector3(px, section.WorkingPlane.O.Y, pz);
            var dir = Vector3.UnitY;
            var upRay = new Ray3(origin - dir * 0.001f, dir);
            var downRay = new Ray3(origin + dir * 0.001f, -dir);
            bool hitUp = fan.ComputeIntersection(upRay, out var upD, out _);
            bool hitDown = fan.ComputeIntersection(downRay, out var downD, out _);
            Console.WriteLine($"fan probe ({px},{pz}): covered={covered} fanY={(covered ? fy.ToString("F2") : "-")} upHit={hitUp}/{upD:F2} downHit={hitDown}/{downD:F2}");
        }
        foreach (var (px, pz) in new[] { (0f, 0f), (10f, 0f), (-10f, 0f), (14f, 0f), (-14f, 0f), (0f, 10f), (0f, -10f), (0f, 14f), (2f, 4f) }) {
            bool r = SampleHeight(roadTris, new(px, pz), out var ry);
            bool s = SampleHeight(sectionTris, new(px, pz), out var sy);
            Console.WriteLine($"probe ({px},{pz}): road={(r ? ry.ToString("F2") : "-")} section={(s ? sy.ToString("F2") : "-")}");
        }

        svg.AppendLine("</svg>");
        File.WriteAllText(@"C:\Users\Oskar\AppData\Local\Temp\trsim_harness\topdown.svg", svg.ToString());
        Console.WriteLine("SVG WRITTEN");
        Console.WriteLine("HARNESS DONE");
    }

    //World X -> svg X, world Z -> svg Y (north up = -Z up => flip Z)
    private static void CollectXZTris(Mesh mesh, List<(Vector2, Vector2, Vector2, float, float, float)> outList) {
        var idx = mesh.Indices;
        var vs = mesh.Vertices;
        for (int t = 0; t <= idx.Count - 3; t += 3) {
            var a = vs[idx[t]].Position;
            var b = vs[idx[t + 1]].Position;
            var c = vs[idx[t + 2]].Position;
            //skip vertical walls (zero XZ area)
            var cross = (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
            if (MathF.Abs(cross) < 1e-6f) continue;
            outList.Add((new(a.X, a.Z), new(b.X, b.Z), new(c.X, c.Z), a.Y, b.Y, c.Y));
        }
    }

    private static bool SampleHeight(List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc)> tris, Vector2 p, out float height) {
        height = float.MinValue;
        foreach (var t in tris) {
            float area = (t.b.X - t.a.X) * (t.c.Y - t.a.Y) - (t.b.Y - t.a.Y) * (t.c.X - t.a.X);
            if (MathF.Abs(area) < 1e-9f) continue;
            float la = ((t.b.X - p.X) * (t.c.Y - p.Y) - (t.b.Y - p.Y) * (t.c.X - p.X)) / area;
            float lb = ((t.c.X - p.X) * (t.a.Y - p.Y) - (t.c.Y - p.Y) * (t.a.X - p.X)) / area;
            float lc = 1 - la - lb;
            if (la < -0.001f || lb < -0.001f || lc < -0.001f) continue;
            var y = t.ya * la + t.yb * lb + t.yc * lc;
            if (y > height) height = y;
        }
        return height > float.MinValue / 2;
    }

    private static bool NearOrigin(MultiMesh mm) {
        foreach (var bin in mm.RenderBins) {
            foreach (var v in bin.Value.Vertices)
                if (MathF.Abs(v.Position.X) < 45 && MathF.Abs(v.Position.Z) < 45) return true;
        }
        return false;
    }

    private static void AppendMesh(StringBuilder svg, MultiMesh mm, string color, float opacity) {
        foreach (var bin in mm.RenderBins)
            AppendMesh(svg, bin.Value, color, opacity);
    }

    private static void AppendMesh(StringBuilder svg, Mesh mesh, string color, float opacity) {
        var idx = mesh.Indices;
        var vs = mesh.Vertices;
        for (int t = 0; t <= idx.Count - 3; t += 3) {
            var a = vs[idx[t]].Position;
            var b = vs[idx[t + 1]].Position;
            var c = vs[idx[t + 2]].Position;
            svg.AppendLine($@"<polygon points=""{a.X:F2},{a.Z:F2} {b.X:F2},{b.Z:F2} {c.X:F2},{c.Z:F2}"" fill=""{color}"" fill-opacity=""{opacity}"" stroke=""none""/>");
        }
    }
}
