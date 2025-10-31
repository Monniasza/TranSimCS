using System;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Roads;
using TranSimCS.Worlds.Building;

namespace TranSimCS.Worlds {
    public static class WorldGenerator {

        public static void SetUpExampleWorld(TSWorld world) {
            //Reset the world
            world.ClearAll();

            //Add some example road nodes and segments
            var node1 = new RoadNode(world, "Node 1", new Vector3(0, 0.1f, 0), RoadNode.AZIMUTH_NORTH);
            var node2 = new RoadNode(world, "Node 2", new Vector3(0, 10.1f, 100), RoadNode.AZIMUTH_NORTH);
            var node3 = new RoadNode(world, "Node 3", new Vector3(0, 0.1f, 200), RoadNode.AZIMUTH_NORTH);
            var node4a = new RoadNode(world, "Node 4a", new Vector3(100, 0.1f, 300), RoadNode.AZIMUTH_EAST);
            var node4b = new RoadNode(world, "Node 4b", new Vector3(0, 0.1f, 300), RoadNode.AZIMUTH_NORTH);
            var node4c = new RoadNode(world, "Node 4c", new Vector3(-100, 0.1f, 300), RoadNode.AZIMUTH_WEST);
            // Generate lanes for each node
            Generator.GenerateLanes(2, node1, 3.5f, 0);
            Generator.GenerateLanes(2, node2, 3.5f, 0);
            Generator.GenerateLanes(4, node3, 3.5f, -3.5f);
            Generator.GenerateLanes(1, node4a, 3.5f, 0);
            Generator.GenerateLanes(2, node4b, 3.5f, 0);
            Generator.GenerateLanes(1, node4c, 3.5f, 0);
            world.RoadNodes.Add(node1);
            world.RoadNodes.Add(node2);
            world.RoadNodes.Add(node3);
            world.RoadNodes.Add(node4a);
            world.RoadNodes.Add(node4b);
            world.RoadNodes.Add(node4c);

            //Set up a fancy road example
            var fancynode1 = new RoadNode(world, "Fancy node 1", new Vector3(50, 0.1f, 0), RoadNode.AZIMUTH_NORTH);
            world.RoadNodes.Add(fancynode1);
            var fancynode2 = new RoadNode(world, "Fancy node 2", new Vector3(50, 0.1f, 100), RoadNode.AZIMUTH_NORTH);
            world.RoadNodes.Add(fancynode2);
            var fancyRoad = world.GetOrMakeRoadStrip(fancynode1.FrontEnd, fancynode2.RearEnd);
            var laneBus = LaneSpec.Bus;
            var laneDefault = LaneSpec.Default;
            var laneBike = LaneSpec.Bicycle;
            var lanePed = LaneSpec.Pedestrian;
            var lanePlatform = LaneSpec.Platform;
            var lanetypes = new LaneSpec[] { lanePed, laneBike, laneDefault, laneDefault, lanePlatform, laneBus };
            var laneSpecs = new LaneSpec[12];
            for (int i = 0; i < lanetypes.Length; i++) {
                var lanetype = lanetypes[i];
                laneSpecs[11 - i] = lanetype;
                lanetype.Flags ^= LaneFlags.Forward | LaneFlags.Backward;
                laneSpecs[i] = lanetype;
            }

            for (int i = 0; i < 12; i++) {
                var loffset = (i - 6) * 3.5f;
                var roffset = loffset + 3.5f;
                var spec = laneSpecs[i];

                var lane1 = new Lane();
                lane1.LeftPosition = loffset;
                lane1.RightPosition = roffset;
                lane1.Spec = spec;
                fancynode1.AddLane(lane1);

                var lane2 = new Lane();
                lane2.LeftPosition = loffset;
                lane2.RightPosition = roffset;
                fancynode2.AddLane(lane2);
                lane2.Spec = spec;

                Generator.JoinLanesByIndices(fancyRoad, i, i, spec);
            }

            //1-2
            var lc12 = Generator.GenerateLaneConnections(node1.FrontEnd, 0, node1.Lanes.Count, node2.RearEnd, 0, node2.Lanes.Count);
            world.RoadSegments.Add(lc12);

            //2-3
            var lc23 = Generator.GenerateLaneConnections(node2.FrontEnd, 0, node2.Lanes.Count, node3.RearEnd, 0, node3.Lanes.Count, 0, 1);
            world.RoadSegments.Add(lc23);

            //3-4a
            var lc34a = Generator.GenerateLaneConnections(node3.FrontEnd, 3, 4, node4a.RearEnd, 0, node4a.Lanes.Count);
            world.RoadSegments.Add(lc34a);

            //3-4b
            var lc34b = Generator.GenerateLaneConnections(node3.FrontEnd, 1, 3, node4b.RearEnd, 0, node4b.Lanes.Count);
            world.RoadSegments.Add(lc34b);

            //3-4c
            var lc34c = Generator.GenerateLaneConnections(node3.FrontEnd, 0, 1, node4c.RearEnd, 0, node4c.Lanes.Count);
            world.RoadSegments.Add(lc34c);

            //Set up an intersection example
            var n10l = new RoadNode(world, "Node 10l", new Vector3(-110, 0.1f, 20), RoadNode.AZIMUTH_NORTH);
            var n10r = new RoadNode(world, "Node 10r", new Vector3(-90, 0.1f, 20), RoadNode.AZIMUTH_NORTH);
            var n11 = new RoadNode(world, "Node 11", new Vector3(-80, 2.1f, 0), RoadNode.AZIMUTH_EAST);
            var n12 = new RoadNode(world, "Node 12", new Vector3(-100, 0.1f, -20), RoadNode.AZIMUTH_SOUTH);
            var n13 = new RoadNode(world, "Node 13", new Vector3(-120, 2.1f, 0), RoadNode.AZIMUTH_WEST);

            Generator.GenerateLanes(2, n10l, 3, -3);
            Generator.GenerateLanes(2, n10r, 3, -3);
            Generator.GenerateLanes(2, n11, 3, -3);
            Generator.GenerateLanes(2, n12, 3, -3);
            Generator.GenerateLanes(2, n13, 3, -3);

            world.RoadNodes.Add(n10l);
            world.RoadNodes.Add(n10r);
            world.RoadNodes.Add(n11);
            world.RoadNodes.Add(n12);
            world.RoadNodes.Add(n13);

            var n10lb = n10l.RearEnd;
            var n10rb = n10r.RearEnd;
            var n11b = n11.RearEnd;
            var n12b = n12.RearEnd;
            var n13b = n13.RearEnd;

            var section = new RoadSection();
            n10lb.ConnectedSection.Value = section;
            n10rb.ConnectedSection.Value = section;
            n11b.ConnectedSection.Value = section;
            n12b.ConnectedSection.Value = section;
            n13b.ConnectedSection.Value = section;
            section.MainSlopeNodes.Value = new RoadNodeEndPair(n11b, n13b);
            world.RoadSections.Add(section);

            //Another array of buildings
            for(int i = 0; i < 128; i++) {
                BuildingUnit building = new BuildingUnit();
                building.PositionProp.Value = new(new(-1024, 0, i * 128), RoadNode.AZIMUTH_SOUTH);
                building.UnitSizeProp.Value = new(128, 256, 4);
                world.Buildings.data.Add(building);
            }

            //One collosal building
            BuildingUnit building2 = new BuildingUnit();
            building2.PositionProp.Value = new ObjPos(new(-512, 0, 2000), 0);
            building2.UnitSizeProp.Value = new(128, 256, 128) ;
            world.Buildings.data.Add(building2);
        }
    }
}