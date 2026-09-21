using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using TranSimCS;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.StripGenerator;
using TranSimCS.Save2;
using TranSimCS.Worlds;

namespace TestWorldGen {
    /// <summary>
    /// Generates the embedded test world JSON files used by TranSimCSTests.
    /// <para>
    /// Run from the repository root:
    /// <c>dotnet run --project tools/TestWorldGen -- TranSimCSTests/TestWorlds</c>
    /// </para>
    /// <para>
    /// The worlds are generated rather than hand-written so that they are guaranteed to match the
    /// serializer's format. The generated files are committed and embedded into the test assembly, so
    /// the tests never read from disk.
    /// </para>
    /// </summary>
    public static class Generator {
        /// <summary>
        /// Entry point. Writes the test worlds into the directory given as the first argument, or into
        /// <c>TranSimCSTests/TestWorlds</c> when no argument is given.
        /// </summary>
        public static void Main(string[] args) {
            var outputDir = args.Length > 0 ? args[0] : "TranSimCSTests/TestWorlds";
            Directory.CreateDirectory(outputDir);

            //Program.DataRoot is normally set by the application's Main, which this tool does not run.
            //TexturePipeline reads it in a static initializer, so it has to be set before TexturePipeline
            //is first touched. The setter is private, so it is set by reflection here.
            var dataRoot = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Files")));
            typeof(Program)
                .GetProperty("DataRoot", BindingFlags.Public | BindingFlags.Static)!
                .SetValue(null, dataRoot);
            Console.WriteLine($"DataRoot = {Program.DataRoot}");

            //Textures must be loaded before any road node is added to a world, because adding a node
            //builds its mesh and the mesh builder rejects a material with no texture.
            Materials.ReadAssets();

            //Register the spline generators, so that a saved world can be loaded back.
            StripSplineGenerator.typeRegistry.Register("isotropic",
                IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(ClassicStripSplineGenerator.Instance));
            StripSplineGenerator.typeRegistry.Register("anisotropic",
                IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(AnisotropicStripSplineGenerator.Instance));

            WriteStraightRoad(outputDir);
        }

        /// <summary>
        /// Writes a straight two-node road with one lane in each direction, and one lane strip per
        /// direction. The forward strip runs from node A to node B, the reverse strip from node B to
        /// node A, so that both strip orientations are covered by the same world.
        /// </summary>
        /// <param name="outputDir">The directory to write the world into.</param>
        private static void WriteStraightRoad(string outputDir) {
            var nodeA = new RoadNode("A", new PositionEulerAngles(new Vector3(0, 0, 0), 0, 0, 0));
            var nodeB = new RoadNode("B", new PositionEulerAngles(new Vector3(0, 0, 100), 0, 0, 0));

            //Two lanes, one on each side of the centre line.
            nodeA.AddLane(new LaneNode(LaneSpec.Default, -1.75f));
            nodeA.AddLane(new LaneNode(LaneSpec.Default, 1.75f));
            nodeB.AddLane(new LaneNode(LaneSpec.Default, -1.75f));
            nodeB.AddLane(new LaneNode(LaneSpec.Default, 1.75f));

            var world = new TSWorld();
            world.Nodes.data.Add(nodeA);
            world.Nodes.data.Add(nodeB);

            var road = world.GetOrMakeRoadStrip(nodeA.FrontHalf, nodeB.RearHalf)
                ?? throw new InvalidOperationException("Failed to create the road strip");

            //Forward strip: lane 0 of A's front half to lane 0 of B's rear half.
            var forward = world.GetOrMakeLaneStrip(nodeA.FrontHalf.SortedLanes[0], nodeB.RearHalf.SortedLanes[0])
                ?? throw new InvalidOperationException("Failed to create the forward lane strip");
            //Reverse strip: lane 1 of B's rear half to lane 1 of A's front half.
            var reverse = world.GetOrMakeLaneStrip(nodeB.RearHalf.SortedLanes[1], nodeA.FrontHalf.SortedLanes[1])
                ?? throw new InvalidOperationException("Failed to create the reverse lane strip");

            //Force the paths into existence so that they are serialized.
            _ = forward.Path;
            _ = reverse.Path;

            var path = Path.Combine(outputDir, "straight-road.json");
            world.SaveToFileJson(path);
            Console.WriteLine($"Wrote {path}");
            Console.WriteLine($"  forward strip {forward.Guid} path {forward.Path?.Guid}");
            Console.WriteLine($"  reverse strip {reverse.Guid} path {reverse.Path?.Guid}");
        }
    }
}
