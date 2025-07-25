﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using System.Collections.ObjectModel;

namespace TranSimCS.Worlds
{
    public class World{
        public ObservableCollection<RoadStrip> RoadSegments { get; } = new();
        public ObservableCollection<RoadNode> RoadNodes { get; } = new();

        public RoadStrip FindRoadStrip(RoadNodeEnd start, RoadNodeEnd end) {
            foreach (var strip in RoadSegments) 
                if (strip.CheckEnds(start, end)) 
                    return strip;
            return null;
        }
        public RoadStrip GetOrMakeRoadStrip(RoadNodeEnd start, RoadNodeEnd end) {
            RoadStrip result = FindRoadStrip(start, end);
            if (result == null) {
                result = new RoadStrip(start, end);
                RoadSegments.Add(result);
            }
            return result;
        }

        public LaneStrip FindLaneStrip(LaneEnd start, LaneEnd end) {
            var roadStrip = FindRoadStrip(start.RoadNodeEnd, end.RoadNodeEnd);
            if (roadStrip == null) return null;
            foreach (var lane in roadStrip.Lanes) 
                if(lane.IsBetween(start, end)) return lane;
            return null;
        }

        public World() {
            RoadSegments.CollectionChanged += RoadSegments_CollectionChanged; // Subscribe to changes in the road segments collection
            RoadNodes.CollectionChanged += RoadNodes_CollectionChanged; // Subscribe to changes in the road nodes collection
        }

        private void RoadSegments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road segments collection if needed
            // For example, you could log changes or update UI elements
            foreach (var segment in e.NewItems?.OfType<RoadStrip>() ?? Enumerable.Empty<RoadStrip>())
                HandleAddRoadSegment(segment); // Handle the addition of a new road segment
            foreach (var segment in e.OldItems?.OfType<RoadStrip>() ?? Enumerable.Empty<RoadStrip>())
                HandleRemoveRoadSegment(segment); // Handle the removal of a road segment
        }
        private void HandleAddRoadSegment(RoadStrip segment) {
            // Handle the addition of a new road segment
            segment.OnLaneRemoved += LaneRemovedFromRoad; // Subscribe to lane removal events in the road segment
            segment.OnLaneAdded += LaneAddedToRoad; // Subscribe to lane addition events in the road segment
            segment.StartNode.connections.Add(segment);
            segment.EndNode.connections.Add(segment);
        }
        private void HandleRemoveRoadSegment(RoadStrip segment) {
            // Handle the removal of a road segment
            segment.OnLaneAdded -= LaneAddedToRoad; // Unsubscribe from lane addition events in the road segment
            segment.OnLaneRemoved -= LaneRemovedFromRoad; // Unsubscribe from lane removal events in the road segment
            segment.StartNode.connections.Remove(segment);
            segment.EndNode.connections.Remove(segment);

            //Remove node connections that are no longer valid
            var lanes = segment.Lanes.ToArray();
            foreach(var lane in lanes){
                lane.Destroy();
            };
        }
        private void LaneAddedToRoad(object sender, RoadStripEventArgs e) {
            //Handle the addition of a new lane to a road segment
        }
        private void LaneRemovedFromRoad(object sender, RoadStripEventArgs e) {
            //Handle the removal of a lane from a road segment
        }

        private void RoadNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            // Handle changes to the road nodes collection if needed
            // For example, you could log changes or update UI elements
            foreach (var node in e.NewItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleAddRoadNode(node); // Handle the addition of a new road node
            foreach (var node in e.OldItems?.OfType<RoadNode>() ?? Enumerable.Empty<RoadNode>())
                HandleRemoveRoadNode(node); // Handle the removal of a road node
        }
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PositionProp.ValueChanged += RoadNodePositionChanged; // Subscribe to changes in the road node position
        }
        private void HandleRemoveRoadNode(RoadNode node) {
            // Handle the removal of a road node
            node.PositionProp.ValueChanged -= RoadNodePositionChanged; // Unsubscribe from changes in the road node position
            foreach (var segment in node.Connections) {
                segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node is removed
                RoadSegments.Remove(segment); // Remove the segment from the road segments collection
            }
        }

        private void RoadNodePositionChanged(object sender, PropertyChangedEventArgs2<ObjPos> e) {
            if(sender is RoadNode node) {
                foreach(var segment in node.Connections) {
                    segment.InvalidateMesh(); // Invalidate the mesh of the segment if the node position changes
                }
            }
        }

        public void Update(float deltaTime)
        {

            // Update logic for the world can be added here
            foreach (var node in RoadNodes)
            {
                // Example: Update each road node's position or state
                
            }
        }

        public static void SetUpExampleWorld(World world) {
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
            var fancyRoad = world.GetOrMakeRoadStrip(fancynode1.FrontEnd, fancynode2.FrontEnd);
            var laneBus = LaneSpec.Bus;
            var laneDefault = LaneSpec.Default;
            var laneBike = LaneSpec.Bicycle;
            var lanePed = LaneSpec.Pedestrian;
            var lanePlatform = LaneSpec.Platform;
            var lanetypes = new LaneSpec[] { lanePed, laneBike, laneDefault, laneDefault, lanePlatform, laneBus};
            var laneSpecs = new LaneSpec[12];
            for(int i = 0; i <  lanetypes.Length; i++) {
                var lanetype = lanetypes[i];
                laneSpecs[11-i] = lanetype;
                lanetype.Flags ^= LaneFlags.Forward | LaneFlags.Backward;
                laneSpecs[i] = lanetype;
            }

            for(int i = 0; i < 12; i++) {
                var loffset = (i - 6) * 3.5f;
                var roffset = loffset + 3.5f;
                var spec = laneSpecs[i];

                var lane1 = new Lane(fancynode1);
                lane1.LeftPosition = loffset;
                lane1.RightPosition = roffset;
                lane1.Spec = spec;
                fancynode1.AddLane(lane1);

                var lane2 = new Lane(fancynode2);
                lane2.LeftPosition = loffset;
                lane2.RightPosition = roffset;
                fancynode2.AddLane(lane2);
                lane2.Spec = spec;

                Generator.JoinLanesByIndices(fancyRoad, i, i, spec);
            }

            //1-2
            var lc12 = Generator.GenerateLaneConnections(node1.FrontEnd, 0, node1.Lanes.Count, node2.FrontEnd, 0, node2.Lanes.Count);
            world.RoadSegments.Add(lc12);

            //2-3
            var lc23 = Generator.GenerateLaneConnections(node2.FrontEnd, 0, node2.Lanes.Count, node3.FrontEnd, 0, node3.Lanes.Count, 0, 1);
            world.RoadSegments.Add(lc23);

            //3-4a
            var lc34a = Generator.GenerateLaneConnections(node3.FrontEnd, 3, 4, node4a.FrontEnd, 0, node4a.Lanes.Count);
            world.RoadSegments.Add(lc34a);

            //3-4b
            var lc34b = Generator.GenerateLaneConnections(node3.FrontEnd, 1, 3, node4b.FrontEnd, 0, node4b.Lanes.Count);
            world.RoadSegments.Add(lc34b);

            //3-4c
            var lc34c = Generator.GenerateLaneConnections(node3.FrontEnd, 0, 1, node4c.FrontEnd, 0, node4c.Lanes.Count);
            world.RoadSegments.Add(lc34c);
        }

        
    }
}
