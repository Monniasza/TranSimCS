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
    private static void Main(string[] args) {
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

        //Pick the section to inspect - overridable via args (1-based)
        int targetSection = args.Length > 0 ? int.Parse(args[0]) : 2;
        var secIt = world.RoadSections.data.GetEnumerator();
        RoadSection? inspected = null;
        int idx = 0;
        while (secIt.MoveNext()) { idx++; if (idx == targetSection) inspected = (RoadSection)secIt.Current; }
        var section = inspected!;
        var c0 = section.Center;
        Console.WriteLine($"inspecting section {targetSection} center={c0}");

        var svg = new StringBuilder();
        svg.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""600"" height=""600"" viewBox=""{c0.X - 30:F0} {c0.Z - 30:F0} 60 60"">");
        svg.AppendLine($@"<rect x=""{c0.X - 30:F0}"" y=""{c0.Z - 30:F0}"" width=""60"" height=""60"" fill=""#4a7a3a""/>");

        //Road strips near the section: height-coloured translucent
        var roadTris = new List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc)>();
        foreach (var road in world.RoadSegments.data) {
            foreach (var lane in road.Lanes) {
                MultiMesh mm;
                try { mm = lane.GetMesh(); } catch { continue; }
                if (!Near(mm, c0, 45)) continue;
                foreach (var bin in mm.RenderBins) {
                    CollectXZTris(bin.Value, roadTris);
                    AppendMesh(svg, bin.Value, y => HeightColor(y), 0.45f);
                }
            }
        }

        //Section surface on top: height-coloured opaque
        MultiMesh smm;
        try { smm = section.Mesh.GetMesh(); } catch (Exception ex) { Console.WriteLine($"section EXCEPTION: {ex.Message}"); return; }
        var sectionTris = new List<(Vector2 a, Vector2 b, Vector2 c, float ya, float yb, float yc)>();
        int binIndex = 0;
        var binColors = new[] { "#ff0000", "#00ff00", "#0000ff", "#ff00ff", "#00ffff", "#ff8000" };
        foreach (var bin in smm.RenderBins) {
            CollectXZTris(bin.Value, sectionTris);
            var opacity = binIndex == 0 ? 0.9f : 0.75f;
            AppendMesh(svg, bin.Value, _ => binColors[binIndex % binColors.Length], opacity);
            binIndex++;
        }

        //All sections: stretched triangles
        int si = 0;
        var sectionIt2 = world.RoadSections.data.GetEnumerator();
        while (sectionIt2.MoveNext()) {
            si++;
            var sec = (RoadSection)sectionIt2.Current;
            MultiMesh mm;
            try { mm = sec.Mesh.GetMesh(); } catch { continue; }
            foreach (var bin in mm.RenderBins) {
                var idxs = bin.Value.Indices;
                var vs = bin.Value.Vertices;
                int stretched = 0;
                for (int t = 0; t <= idxs.Count - 3; t += 3) {
                    var a = vs[idxs[t]].Position;
                    var b = vs[idxs[t + 1]].Position;
                    var c = vs[idxs[t + 2]].Position;
                    var vSpread = MathF.Max(MathF.Abs(a.Y - b.Y), MathF.Max(MathF.Abs(b.Y - c.Y), MathF.Abs(a.Y - c.Y)));
                    var xzSpread = MathF.Max(MathF.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z)), MathF.Max(MathF.Sqrt((b.X - c.X) * (b.X - c.X) + (b.Z - c.Z) * (b.Z - c.Z)), MathF.Sqrt((a.X - c.X) * (a.X - c.X) + (a.Z - c.Z) * (a.Z - c.Z))));
                    if (vSpread > 0.5f && xzSpread < vSpread * 0.8f) {
                        stretched++;
                        if (stretched <= 2) Console.WriteLine($"section {si} STRETCHED: A=({a.X:F2},{a.Y:F2},{a.Z:F2}) B=({b.X:F2},{b.Y:F2},{b.Z:F2}) C=({c.X:F2},{c.Y:F2},{c.Z:F2})");
                    }
                }
                if (stretched > 0) Console.WriteLine($"section {si}: {stretched} stretched triangles");
            }
        }

        //Crease detection per bin: adjacent triangles whose normals disagree - the ridges the user sees
        int bidx = 0;
        foreach (var bin in smm.RenderBins) {
            if (bin.Value.Indices.Count == 0) continue;
            Console.WriteLine($"bin verts={bin.Value.Vertices.Count} tris={bin.Value.Indices.Count / 3} tex={bin.Key.Texture}");
            ReportCreases(bin.Value, c0);
            if (bidx == 0) {
                //dump every triangle touching the west-mouth lane edge (z≈±1.5, x≈221)
                var vss = bin.Value.Vertices;
                var ixs = bin.Value.Indices;
                for (int t = 0; t <= ixs.Count - 3; t += 3) {
                    var a = vss[ixs[t]].Position;
                    var b = vss[ixs[t + 1]].Position;
                    var c = vss[ixs[t + 2]].Position;
                    var mx = (a.X + b.X + c.X) / 3;
                    var mz = (a.Z + b.Z + c.Z) / 3;
                    if (MathF.Abs(mx - (c0.X + 0.4f)) < 1.2f && MathF.Abs(MathF.Abs(mz - c0.Z) - 1.53f) < 0.3f)
                        Console.WriteLine($"  tri@({mx:F2},{mz:F2}): A=({a.X:F2},{a.Y:F2},{a.Z:F2}) B=({b.X:F2},{b.Y:F2},{b.Z:F2}) C=({c.X:F2},{c.Y:F2},{c.Z:F2})");
                }
            }
            bidx++;
        }

        //Grid sweep: section surface vs road surface height around the section centre
        int hover = 0, fight = 0, gaps = 0;
        for (float gx = c0.X - 30; gx <= c0.X + 30; gx += 0.5f) {
            for (float gz = c0.Z - 30; gz <= c0.Z + 30; gz += 0.5f) {
                var p = new Vector2(gx, gz);
                bool secHas = SampleHeight(sectionTris, p, out var secY);
                bool roadHas = SampleHeight(roadTris, p, out var roadY);
                if (secHas && roadHas) {
                    var d = secY - roadY;
                    if (d > 0.1f) { hover++; if (hover < 10) Console.WriteLine($"HOVER at ({gx:F1},{gz:F1}): section {secY:F2} vs road {roadY:F2} (d={d:F2})"); }
                    else if (MathF.Abs(d) <= 0.1f) fight++;
                } else if (secHas) gaps++;
            }
        }
        Console.WriteLine($"SWEEP: hover={hover} fight={fight} sectionOnly={gaps}");

        svg.AppendLine("</svg>");
        File.WriteAllText("topdown.svg", svg.ToString());
        Console.WriteLine("SVG WRITTEN");
        Console.WriteLine("HARNESS DONE");
    }

    //Finds adjacent triangles whose normals disagree - the ridges the user sees in-game
    private static void ReportCreases(Mesh bin, Vector3 center) {
        var idx = bin.Indices;
        var vs = bin.Vertices;

        //map "edge" (sorted position pair quantised) -> list of triangle normals
        var edges = new Dictionary<(long, long, long, long), List<Vector3>>();
        for (int t = 0; t <= idx.Count - 3; t += 3) {
            var a = vs[idx[t]].Position;
            var b = vs[idx[t + 1]].Position;
            var c = vs[idx[t + 2]].Position;
            var n = Vector3.Cross(b - a, c - a);
            if (n.LengthSquared() < 1e-12f) continue;
            n = Vector3.Normalize(n);
            foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) }) {
                var k = (Quant(p.X), Quant(p.Z), Quant(q.X), Quant(q.Z));
                if (k.Item1 > k.Item3 || (k.Item1 == k.Item3 && k.Item2 > k.Item4))
                    k = (Quant(q.X), Quant(q.Z), Quant(p.X), Quant(p.Z));
                if (!edges.TryGetValue(k, out var list)) { list = new(); edges[k] = list; }
                list.Add(n);
            }
        }

        //interior edges shared by 2 triangles: measure normal flip (upward normals expected)
        int creases = 0;
        var worst = new List<(float angle, Vector3 mid)>();
        foreach (var kv in edges) {
            if (kv.Value.Count != 2) continue;
            var dot = Vector3.Dot(kv.Value[0], kv.Value[1]);
            var angle = MathF.Acos(Math.Clamp(dot, -1, 1)) * 180 / MathF.PI;
            if (angle > 20) {
                creases++;
                var mid = new Vector3((Unquant(kv.Key.Item1) + Unquant(kv.Key.Item3)) / 2, 0, (Unquant(kv.Key.Item2) + Unquant(kv.Key.Item4)) / 2);
                worst.Add((angle, mid));
            }
        }
        Console.WriteLine($"  {creases} creases >20deg among {edges.Count(e => e.Value.Count == 2)} interior edges");
        foreach (var w in worst.OrderByDescending(w => w.angle).Take(8))
            Console.WriteLine($"  crease {w.angle:F0}deg at ({w.mid.X:F2},{w.mid.Z:F2}) rel=({w.mid.X - center.X:F2},{w.mid.Z - center.Z:F2})");
    }

    private static long Quant(float v) => (long)MathF.Round(v * 50f);
    private static float Unquant(long v) => v / 50f;

    private static string HeightColor(float y) {
        var t = Math.Clamp((y + 10f) / 17f, 0f, 1f);
        return $"rgb({(int)(235 * (1 - t))},{(int)(235 * (1 - t))},255)";
    }

    private static void CollectXZTris(Mesh mesh, List<(Vector2, Vector2, Vector2, float, float, float)> outList) {
        var idx = mesh.Indices;
        var vs = mesh.Vertices;
        for (int t = 0; t <= idx.Count - 3; t += 3) {
            var a = vs[idx[t]].Position;
            var b = vs[idx[t + 1]].Position;
            var c = vs[idx[t + 2]].Position;
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

    private static bool Near(MultiMesh mm, Vector3 center, float radius) {
        foreach (var bin in mm.RenderBins) {
            foreach (var v in bin.Value.Vertices)
                if (Vector3.DistanceSquared(v.Position, center) < radius * radius) return true;
        }
        return false;
    }

    private static void AppendMesh(StringBuilder svg, MultiMesh mm, Func<float, string> color, float opacity) {
        foreach (var bin in mm.RenderBins)
            AppendMesh(svg, bin.Value, color, opacity);
    }

    private static void AppendMesh(StringBuilder svg, Mesh mesh, Func<float, string> color, float opacity) {
        var idx = mesh.Indices;
        var vs = mesh.Vertices;
        for (int t = 0; t <= idx.Count - 3; t += 3) {
            var a = vs[idx[t]].Position;
            var b = vs[idx[t + 1]].Position;
            var c = vs[idx[t + 2]].Position;
            var y = (a.Y + b.Y + c.Y) / 3;
            svg.AppendLine($@"<polygon points=""{a.X:F2},{a.Z:F2} {b.X:F2},{b.Z:F2} {c.X:F2},{c.Z:F2}"" fill=""{color(y)}"" fill-opacity=""{opacity}"" stroke=""none""/>");
        }
    }
}
