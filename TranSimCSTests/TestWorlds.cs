using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TranSimCS;
using TranSimCS.Cars;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.ModelOld;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Roads.StripGenerator;
using TranSimCS.Save2;
using TranSimCS.SilkNet;
using TranSimCS.Worlds;

namespace TranSimCSTests {
    /// <summary>
    /// Loads the test worlds that are embedded into the test assembly.
    /// <para>
    /// The worlds are embedded resources rather than files on disk, so the tests do not depend on the
    /// working directory or on files being copied to the output folder. Regenerate them with
    /// <c>dotnet run --project tools/TestWorldGen -- TranSimCSTests/TestWorlds</c>.
    /// </para>
    /// </summary>
    public static class TestWorlds {
        /// <summary>
        /// The name of the embedded world containing a straight two-node road with one lane strip in
        /// each direction.
        /// </summary>
        public const string StraightRoad = "straight-road.json";

        private static readonly Assembly Assembly = typeof(TestWorlds).Assembly;

        /// <summary>
        /// Reads an embedded test world as a UTF-8 string.
        /// </summary>
        /// <param name="name">The file name of the world, for example <see cref="StraightRoad"/>.</param>
        /// <returns>The world's JSON text.</returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when no embedded resource with that name exists.
        /// </exception>
        public static string ReadJson(string name) {
            var resourceName = Assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException(
                    $"Embedded test world '{name}' was not found. Available: " +
                    string.Join(", ", Assembly.GetManifestResourceNames()));

            using var stream = Assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Loads an embedded test world into a new <see cref="TSWorld"/>.
        /// <para>
        /// The spline generators are registered before loading, because a saved road strip refers to its
        /// generator by name and the loader needs the registry to resolve it.
        /// </para>
        /// </summary>
        /// <param name="name">The file name of the world, for example <see cref="StraightRoad"/>.</param>
        /// <returns>The loaded world.</returns>
        public static TSWorld Load(string name) {
            RegisterSplineGenerators();
            InitializeMaterials();
            InitializeCarModels();
            JsonProcessor.Init();

            var json = ReadJson(name);
            var world = new TSWorld();
            var options = world.CreateJsonOptions();
            var readerOptions = new JsonReaderOptions {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), readerOptions);
            world.LoadJsonData(ref reader, options);
            return world;
        }

        private static bool splineGeneratorsRegistered;
        private static readonly object splineGeneratorsLock = new();

        /// <summary>
        /// Registers the built-in spline generators in the type registry.
        /// <para>
        /// The registry rejects a duplicate name, so this registers only once per process. It is thread
        /// safe, because xUnit runs test classes in parallel.
        /// </para>
        /// </summary>
        public static void RegisterSplineGenerators() {
            if (splineGeneratorsRegistered) return;
            lock (splineGeneratorsLock) {
                if (splineGeneratorsRegistered) return;
                StripSplineGenerator.typeRegistry.Register("isotropic",
                    IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(ClassicStripSplineGenerator.Instance));
                StripSplineGenerator.typeRegistry.Register("anisotropic",
                    IgnoreSavedTokenConverter<StripSplineGenerator>.FromConstant(AnisotropicStripSplineGenerator.Instance));
                splineGeneratorsRegistered = true;
            }
        }

        private static bool materialsInitialized;
        private static readonly object materialsLock = new();

        /// <summary>
        /// Populates <see cref="Materials"/> with synthetic textures.
        /// <para>
        /// Adding a road node to a world builds that node's mesh, and the mesh builder rejects a material
        /// with no texture. The real <see cref="Materials.ReadAssets"/> loads textures from disk, which
        /// needs <c>Program.DataRoot</c> and is not available in a test host, so the materials are filled
        /// in here with a one-pixel texture instead.
        /// </para>
        /// <para>
        /// This is idempotent and thread safe, so it is safe to call from every test.
        /// </para>
        /// </summary>
        public static void InitializeMaterials() {
            if (materialsInitialized) return;
            lock (materialsLock) {
                if (materialsInitialized) return;

                var texture = new TextureData(1, 1, TextureFormat.RGBA8, new byte[] { 255, 255, 255, 255 });
                var material = new SimpleMaterial { Texture = texture, Emissive = texture };

                var flags = BindingFlags.Public | BindingFlags.Static;
                foreach (var property in typeof(Materials).GetProperties(flags)) {
                    if (property.PropertyType != typeof(SimpleMaterial)) continue;
                    if (!property.CanWrite) continue;
                    property.SetValue(null, material);
                }
                foreach (var property in typeof(Materials).GetProperties(flags)) {
                    if (property.PropertyType != typeof(TextureData)) continue;
                    if (!property.CanWrite) continue;
                    property.SetValue(null, texture);
                }

                materialsInitialized = true;
            }
        }

        private static bool carModelsInitialized;
        private static readonly object carModelsLock = new();

        /// <summary>
        /// Registers the synthetic car model, so that cars can be created in tests.
        /// <para>
        /// The real <see cref="Car.Init"/> loads car models from disk, which needs
        /// <c>Program.DataRoot</c> and is not available in a test host. Only the synthetic model is
        /// registered here, because that is the one <see cref="Car.Randomize"/> selects.
        /// </para>
        /// <para>
        /// This is idempotent and thread safe, so it is safe to call from every test.
        /// </para>
        /// </summary>
        public static void InitializeCarModels() {
            if (carModelsInitialized) return;
            lock (carModelsLock) {
                if (carModelsInitialized) return;
                if (!Car.loadedMeshes.ContainsKey("synthetic")) {
                    //The real CarModel.CreateModel loads its textures from disk, which needs
                    //Program.DataRoot and is not available in a test host. A minimal mesh with a
                    //synthetic material is enough, because the tests only need a car that can be
                    //positioned and updated.
                    var mesh = new Mesh();
                    mesh.AddVerts([
                        new Vertex(new System.Numerics.Vector3(-1, 0, -1), Colors.White, System.Numerics.Vector2.Zero),
                        new Vertex(new System.Numerics.Vector3(1, 0, -1), Colors.White, System.Numerics.Vector2.Zero),
                        new Vertex(new System.Numerics.Vector3(0, 0, 1), Colors.White, System.Numerics.Vector2.Zero)
                    ]);
                    mesh.DrawTriangle(0, 1, 2);

                    var instance = new MeshUnroll.MeshDrawInstance(
                        mesh,
                        TransformQ.Identity,
                        new SimpleMaterial { Texture = Materials.WhiteTex, Emissive = Materials.WhiteTex },
                        0);

                    Car.loadedMeshes.Add("synthetic", instance);
                    Car.meshes.Add(("synthetic", instance));
                }
                carModelsInitialized = true;
            }
        }

        /// <summary>
        /// Finds the single road strip in a world.
        /// </summary>
        /// <param name="world">The world to search.</param>
        /// <returns>The only road strip in the world.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the world does not contain exactly one road strip.
        /// </exception>
        public static RoadStrip SingleRoad(TSWorld world) {
            var roads = world.RoadSegments.data.ToArray();
            if (roads.Length != 1)
                throw new InvalidOperationException($"Expected exactly one road strip, found {roads.Length}");
            return roads[0];
        }

        /// <summary>
        /// Finds the lane strip in a world that runs in the direction of its road.
        /// </summary>
        /// <param name="world">The world to search.</param>
        /// <returns>The forward-running lane strip.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the world does not contain exactly one forward-running lane strip.
        /// </exception>
        public static LaneStrip ForwardStrip(TSWorld world) {
            var strips = SingleRoad(world).Lanes.Where(x => !x.IsReverse()).ToArray();
            if (strips.Length != 1)
                throw new InvalidOperationException($"Expected exactly one forward strip, found {strips.Length}");
            return strips[0];
        }

        /// <summary>
        /// Finds the lane strip in a world that runs against the direction of its road.
        /// </summary>
        /// <param name="world">The world to search.</param>
        /// <returns>The reverse-running lane strip.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the world does not contain exactly one reverse-running lane strip.
        /// </exception>
        public static LaneStrip ReverseStrip(TSWorld world) {
            var strips = SingleRoad(world).Lanes.Where(x => x.IsReverse()).ToArray();
            if (strips.Length != 1)
                throw new InvalidOperationException($"Expected exactly one reverse strip, found {strips.Length}");
            return strips[0];
        }
    }
}
