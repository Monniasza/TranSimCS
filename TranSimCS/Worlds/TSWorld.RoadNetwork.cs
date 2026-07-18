using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoGame.Extended;
using TranSimCS.Collections;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Section;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Worlds {
    public partial class TSWorld {
        //Road contents
        public SegmentStack RoadSegments { get; }
        public SectionStack RoadSections { get; }
        public NodeStack Nodes { get; }

        //Segment handlers
        private void HandleAddRoadSegment(RoadStrip segment) {
            //Add missing dependencies
            AddIfAbsent(segment.StartNode.Node);
            AddIfAbsent(segment.EndNode.Node);

            //Add road segment listeners
            segment.PropertyChanged += HandleChangeRoadSegment;

            //Link the road strip to half-nodes
            var startHalf = segment.StartNode.Node.GetHalfNode(segment.StartNode.End);
            var endHalf = segment.EndNode.Node.GetHalfNode(segment.EndNode.End);
            segment.StartNode.connectedSegments.Add(segment);
            segment.EndNode.connectedSegments.Add(segment);
            startHalf._connectedRoadStrips.Add(new(segment, SegmentHalf.Start));
            endHalf._connectedRoadStrips.Add(new(segment, SegmentHalf.End));

            //Link the road strip to a section
            var sectionA = segment.StartNode.ConnectedSection.Value;
            var sectionB = segment.EndNode.ConnectedSection.Value;
            if(sectionA != null && sectionB == sectionA) {
                //The segment belongs to a road section
                segment.Section = sectionA;
                sectionA._containedSegments.Add(segment);
            }

            //Fire dependency events
            segment.Section?.FireDependencyEvent(segment.Section, segment, PropertyNames.SegmentOfSection);
            if(segment.Section != null) segment.FireDependencyEvent(segment, segment.Section, PropertyNames.SectionOfSegment);
            segment.FirePropertyEvent(segment, new(PropertyNames.World));
        }

        private void HandleRemoveRoadSegment(RoadStrip segment) {
            //Unlink road strip and section
            var section = segment.Section;
            segment.Section = null;
            section?._containedSegments.Remove(segment);

            //Remove the road segment from nodes
            var startHalf = segment.StartNode.Node.GetHalfNode(segment.StartNode.End);
            var endHalf = segment.EndNode.Node.GetHalfNode(segment.EndNode.End);
            segment.StartNode.connectedSegments.Remove(segment);
            segment.EndNode.connectedSegments.Remove(segment);
            startHalf._connectedRoadStrips.Remove(new(segment, SegmentHalf.Start));
            endHalf._connectedRoadStrips.Remove(new(segment, SegmentHalf.End));

            //Remove connections from the road segment. More events will be fired.
            var lanes = segment.Lanes.ToArray();
            foreach (var lane in lanes) lane.Destroy();

            //Remove road segment listeners
            segment.PropertyChanged -= HandleChangeRoadSegment;

            //Fire dependency events
            section?.FireDependencyEvent(section, segment, PropertyNames.SegmentOfSection);
            if (section != null) segment.FireDependencyEvent(segment, section, PropertyNames.SectionOfSegment);
            segment.FirePropertyEvent(segment, new(PropertyNames.DeleteFromWorld));
        }

        private void HandleChangeRoadSegment(object? sender, PropertyChangedEventArgs e) {
            if (sender is not RoadStrip segment) {
                Debug.Fail("Fired for non-segment object");
                return;
            }
            var section = segment.Section;
            segment.StartNode.Node.FireDependencyEvent(segment.StartNode.Node, segment, "connections");
            segment.EndNode.Node.FireDependencyEvent(segment.EndNode.Node, segment, "connections");
            section?.FireDependencyEvent(section, segment, PropertyNames.SegmentOfSection);
        }

        //Section handlers
        private void HandleAddRoadSection(RoadSection section) {
            section.FirePropertyEvent(this, new(PropertyNames.World));

        }
        private void HandleRemoveRoadSection(RoadSection section) {
            var segments = section._containedSegments.ToArray();

            //Remove section from road segments
            foreach(var segment in segments) {
                Debug.Assert(segment.Section == section, "Segment is contained in a section where the segment does not hold the section");
                segment.Section = null;
            }
            section._containedSegments.Clear();

            //Fire dependency events
            foreach (var segment in segments) {
                segment.FireDependencyEvent(segment, section, PropertyNames.SectionOfSegment);
                section.FireDependencyEvent(section, segment, PropertyNames.SegmentOfSection);
            }
            section.FirePropertyEvent(this, new(PropertyNames.DeleteFromWorld));
        }

        //Node handlers
        public HashSet<Obj?> FindRoadNodeDependencies(RoadNode roadNode) => [
            roadNode.FrontHalf.ConnectedSection.Value,
            roadNode.RearHalf.ConnectedSection.Value,
            .. roadNode.Connections,
        ];
        private void HandleAddRoadNode(RoadNode node) {
            // Handle the addition of a new road node
            node.PropertyChanged += HandleChangeRoadNode;
            node.FrontHalf.ConnectedSection.ValueChanged += HandleSectionRoadNode;
            node.RearHalf.ConnectedSection.ValueChanged += HandleSectionRoadNode;
            HandleChangeRoadNode(node, new PropertyChangedEventArgs("world"));
            Debug.WriteLine($"Road node id {node.Guid} name {node.Name} added");
        }

        private void HandleChangeRoadNode(object? sender, PropertyChangedEventArgs e) {
            if(sender is not RoadNode roadNode) {
                Debug.Fail("Fired for non-node object");
                return;
            }
            HashSet<Obj?> deps = FindRoadNodeDependencies(roadNode);
            foreach (Obj? obj in deps) obj?.FireDependencyEvent(roadNode, obj, e.PropertyName);
        }

        private void HandleRemoveRoadNode(RoadNode node) {
            //Disconnect all sections
            var frontSection = node.FrontHalf.ConnectedSection;
            var rearSection = node.RearHalf.ConnectedSection;
            node.FrontEnd.ConnectedSection.Value = null;
            node.RearEnd.ConnectedSection.Value = null;

            //Delete all connected road strips
            foreach (var segment in node.Connections) {
                RoadSegments.data.Remove(segment); // Remove the segment from the road segments collection
            }
            log.Trace($"Road node id {node.Guid} name {node.Name} removed");

            //Remove node listeners
            node.PropertyChanged -= HandleChangeRoadNode;
            node.FrontHalf.ConnectedSection.ValueChanged -= HandleSectionRoadNode;
            node.RearHalf.ConnectedSection?.ValueChanged -= HandleSectionRoadNode;

            //Fire events
            node.FirePropertyEvent(node, new(PropertyNames.DeleteFromWorld));
        }

        private void HandleSectionRoadNode(IProperty<RoadSection?> sender, RoadSection? oldValue, RoadSection? newValue) {
            if(sender is not Property<RoadSection?> prop) {
                Debug.Fail("The sender property is not a Property<RoadSection?>");
                return;
            }
            if(prop.Parent is not RoadNode roadNode) {
                Debug.Fail("The parent of the property is not a road node");
                return;
            }
            var halfNode = prop.name switch {
                PropertyNames.RearSection => roadNode.RearHalf,
                PropertyNames.FrontSection => roadNode.FrontHalf,
                _ => throw new ArgumentException("The property is not a section property")
            };

            //Add missing dependencies
            AddIfAbsent(roadNode);

            //Remove sections from road strips and vice versa for the old value
            var roadStrips = halfNode.ConnectedRoadStrips.Select(x => x.RoadStrip).ToArray();
            foreach(var segment in roadStrips) {
                oldValue?._containedSegments.Remove(segment);
                segment.Section = null;
            }

            //Check segments for newly assigned road sections
            if(newValue != null) foreach (var segment in roadStrips) {
                if(segment.StartNode.ConnectedSection.Value == newValue && segment.EndNode.ConnectedSection.Value == newValue) {
                    segment.Section = newValue;
                    newValue._containedSegments.Add(segment);
                }
            }

            //Fire dependency events
            oldValue?.FireDependencyEvent(oldValue, prop.Parent, PropertyNames.SegmentOfSection);
            newValue?.FireDependencyEvent(newValue, prop.Parent, PropertyNames.SegmentOfSection);
            foreach (var connection in halfNode.ConnectedRoadStrips) {
                roadNode.FireDependencyEvent(roadNode, connection.RoadStrip, prop.name);
                connection.RoadStrip.FireDependencyEvent(connection.RoadStrip, roadNode, prop.name);
            }
            foreach(var roadStrip in roadStrips) {
                if (oldValue != null) roadStrip.FireDependencyEvent(roadStrip, oldValue, PropertyNames.SectionOfSegment);
                if (newValue != null) roadStrip.FireDependencyEvent(roadStrip, newValue, PropertyNames.SectionOfSegment);
            }
        }
    }
}
