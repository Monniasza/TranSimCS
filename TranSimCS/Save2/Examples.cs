using System;
using System.IO;
using TranSimCS.Roads;
using TranSimCS.Worlds;
using Microsoft.Xna.Framework;
using TranSimCS.Roads.Node;

namespace TranSimCS.Save2.Examples {
    /// <summary>
    /// Examples of using the new save and load system
    /// </summary>
    public static class SaveLoadExamples {

        /// <summary>
        /// Example 1: Basic save and load
        /// </summary>
        public static void Example1_BasicSaveLoad() {
            // Create a new world
            var world = new TSWorld();

            // Add road nodes
            var node1 = new RoadNode(world, "Node 1", new Vector3(0, 0, 0), RoadNode.AZIMUTH_NORTH);
            var lane1 = new Lane {
                LeftPosition = -3.5f,
                RightPosition = 0,
                Spec = LaneSpec.Default
            };
            node1.AddLane(lane1);
            world.Nodes.data.Add(node1);

            var node2 = new RoadNode(world, "Node 2", new Vector3(100, 0, 0), RoadNode.AZIMUTH_NORTH);
            var lane2 = new Lane {
                LeftPosition = -3.5f,
                RightPosition = 0,
                Spec = LaneSpec.Default
            };
            node2.AddLane(lane2);
            world.Nodes.data.Add(node2);

            // Save the world (NEW METHOD - System.Text.Json)
            string savePath = Path.Combine(Program.SaveRoot, "example_world.json");
            world.SaveToFileJson(savePath);
            Console.WriteLine($"World saved to: {savePath}");

            // Load the world (NEW METHOD - System.Text.Json)
            var loadedWorld = TSWorld.LoadJson(savePath);
            Console.WriteLine($"World loaded. Number of nodes: {loadedWorld.Nodes.data.Count}");
        }

        /// <summary>
        /// Example 2: Save with custom options
        /// </summary>
        public static void Example2_CustomOptions() {
            var world = new TSWorld();

            // Create serialization options
            var options = world.CreateJsonOptions();
            options.WriteIndented = true; // Pretty formatting

            // Save with custom options
            string savePath = Path.Combine(Program.SaveRoot, "custom_world.json");
            Program.SerializeToFileJson(savePath, world, options);

            Console.WriteLine($"World saved with custom options to: {savePath}");
        }

        /// <summary>
        /// Example 3: Load into existing object
        /// </summary>
        public static void Example3_LoadIntoExisting() {
            var world = new TSWorld();

            // Add some data
            var node = new RoadNode(world, "Test Node", new Vector3(50, 0, 50), RoadNode.AZIMUTH_EAST);
            world.Nodes.data.Add(node);

            // Save
            string savePath = Path.Combine(Program.SaveRoot, "temp_world.json");
            world.SaveToFileJson(savePath);

            // Create a new world object
            var newWorld = new TSWorld();

            // Load data into existing object
            newWorld.LoadFromFileJson(savePath);

            Console.WriteLine($"Data loaded into existing object. Nodes: {newWorld.Nodes.data.Count}");
        }

        /// <summary>
        /// Example 4: Compare old and new methods
        /// </summary>
        public static void Example4_CompareOldAndNew() {
            var world = new TSWorld();

            // Add some data
            for (int i = 0; i < 10; i++) {
                var node = new RoadNode(world, $"Node {i}", new Vector3(i * 10, 0, 0), RoadNode.AZIMUTH_NORTH);
                var lane = new Lane {
                    LeftPosition = -3.5f,
                    RightPosition = 0,
                    Spec = LaneSpec.Default
                };
                node.AddLane(lane);
                world.Nodes.data.Add(node);
            }

            // OLD METHOD (Newtonsoft.Json)
            var oldPath = Path.Combine(Program.SaveRoot, "old_method.json");
            var sw1 = System.Diagnostics.Stopwatch.StartNew();
            world.SaveToFile(oldPath);
            sw1.Stop();
            var oldSize = new FileInfo(oldPath).Length;

            // NEW METHOD (System.Text.Json)
            var newPath = Path.Combine(Program.SaveRoot, "new_method.json");
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            world.SaveToFileJson(newPath);
            sw2.Stop();
            var newSize = new FileInfo(newPath).Length;

            Console.WriteLine("=== METHOD COMPARISON ===");
            Console.WriteLine($"Old method (Newtonsoft.Json):");
            Console.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Size: {oldSize} bytes");
            Console.WriteLine($"New method (System.Text.Json):");
            Console.WriteLine($"  Time: {sw2.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Size: {newSize} bytes");
            Console.WriteLine($"Speedup: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
        }

        /// <summary>
        /// Example 5: Error handling
        /// </summary>
        public static void Example5_ErrorHandling() {
            try {
                // Try to load a non-existent file
                var world = TSWorld.LoadJson("nonexistent_file.json");
            } catch (FileNotFoundException ex) {
                Console.WriteLine($"Error: File not found - {ex.Message}");
            } catch (System.Text.Json.JsonException ex) {
                Console.WriteLine($"JSON parsing error: {ex.Message}");
            } catch (Exception ex) {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }

            // Safe loading with check
            string safePath = Path.Combine(Program.SaveRoot, "safe_world.json");
            if (File.Exists(safePath)) {
                try {
                    var world = TSWorld.LoadJson(safePath);
                    Console.WriteLine("World loaded successfully!");
                } catch (Exception ex) {
                    Console.WriteLine($"Failed to load world: {ex.Message}");
                }
            } else {
                Console.WriteLine("Save file doesn't exist, creating new world...");
                var world = new TSWorld();
                world.SaveToFileJson(safePath);
            }
        }
    }
}
