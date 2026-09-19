using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using TranSimCS;
using TranSimCS.Model;
using TranSimCS.Save2;
using TranSimCS.Collections;
using TranSimCS.Roads.Section;
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

        var world = TSWorld.LoadFromFile(@"C:\Users\Oskar\AppData\Roaming\TranSim\saves\interchanges with traffic lights 2.transim");
        Console.WriteLine($"nodes={world.Nodes.data.Count} sections={world.RoadSections.data.Count}");

        int i = 0;
        foreach (var section in world.RoadSections.data) {
            i++;
            var center = section.Center;
            var normal = section.Normal;
            var plane = section.WorkingPlane;
            Console.WriteLine($"section {i}: nodes={section.Nodes.Count} revNodes={section.Nodes.Rev().ToArray().Length} center=({center.X:F2},{center.Y:F2},{center.Z:F2}) normal=({normal.X:F3},{normal.Y:F3},{normal.Z:F3})");
            MultiMesh mm;
            try {
                mm = section.Mesh.GetMesh();
            } catch (Exception ex) {
                Console.WriteLine($"  EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                continue;
            }
            foreach (var bin in mm.RenderBins) {
                float minOff = float.MaxValue, maxOff = float.MinValue;
                foreach (var v in bin.Value.Vertices) {
                    var off = Vector3.Dot(v.Position - plane.O, normal);
                    if (off < minOff) minOff = off;
                    if (off > maxOff) maxOff = off;
                }
                Console.WriteLine($"  bin[{bin.Key.Texture?.ToString() ?? "?"}]: verts={bin.Value.Vertices.Count} tris={bin.Value.Indices.Count / 3} offsetAlongNormal=[{minOff:F3} .. {maxOff:F3}]");
            }
        }
        Console.WriteLine("HARNESS DONE");
    }
}
