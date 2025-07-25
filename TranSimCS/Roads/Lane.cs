﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds;

namespace TranSimCS.Roads {
    public struct LaneSpec : IEquatable<LaneSpec> {
        public Color Color { get; set; } // Color of the lane
        public VehicleTypes VehicleTypes { get; set; } // Types of vehicles allowed in the lane
        public LaneFlags Flags { get; set; } // Flags for additional lane properties
        public float Width { get; set; } //Width. Ignored by nodes, but used to store new lane widths
        public float SpeedLimit { get; set; } //Speed limit [km/h]


        // Constructor to initialize the LaneSpec with lane index, width, and offset
        public LaneSpec(Color color, VehicleTypes vehicleTypes, float width = 3.5f, float speedLimit = 50, LaneFlags flags = LaneFlags.Forward) {
            Color = color;
            VehicleTypes = vehicleTypes;
            Flags = flags;
            Width = width;
            SpeedLimit = speedLimit;
        }

        //Common presets for lane specifications
        public static LaneSpec Default => new(Color.Gray, VehicleTypes.Vehicles, 3f, 50);
        public static LaneSpec Motorway => new(Color.DarkGray, VehicleTypes.MotorVehicles, 3.5f, 150);
        public static LaneSpec Bicycle => new(Color.Green, VehicleTypes.Bicycle, 2, 30);
        public static LaneSpec Pedestrian => new(Color.LightGray, VehicleTypes.Pedestrian, 1.5f, 16);
        public static LaneSpec Path => new(Color.LightGray, VehicleTypes.Path, 3,20);
        public static LaneSpec Bus => new(Color.Red, VehicleTypes.Bus, 3, 80);
        public static LaneSpec None => new(Color.Transparent, VehicleTypes.None, 3, 0);
        public static LaneSpec All => new(Color.White, VehicleTypes.All, 3, 100); // All vehicle types allowed
        public static LaneSpec Platform => new(Color.LightGoldenrodYellow, VehicleTypes.Pedestrian, 3, 10, LaneFlags.Platform);

        public override bool Equals(object obj) {
            return obj is LaneSpec spec && Equals(spec);
        }

        public bool Equals(LaneSpec other) {
            return Color.Equals(other.Color) &&
                   VehicleTypes == other.VehicleTypes &&
                   Flags == other.Flags &&
                   Width == other.Width &&
                   SpeedLimit == other.SpeedLimit;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Color, VehicleTypes, Flags, Width, SpeedLimit);
        }

        public static bool operator ==(LaneSpec left, LaneSpec right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneSpec left, LaneSpec right) {
            return !(left == right);
        }
    }

    public struct LaneEnd(NodeEnd End, Lane Lane) : IEquatable<LaneEnd>, IDraggableObj {
        //DRAGGING
        public void Drag(Vector3 vector) => lane.Drag(vector);

        public NodeEnd end = End;
        public Lane lane = Lane;

        public RoadNodeEnd RoadNodeEnd => lane.RoadNode.GetEnd(end);

        public LaneEnd OppositeEnd => new LaneEnd(end.Negate(), lane);

        public override bool Equals(object obj) {
            return obj is LaneEnd end && Equals(end);
        }

        public bool Equals(LaneEnd other) {
            return end == other.end &&
                   EqualityComparer<Lane>.Default.Equals(lane, other.lane);
        }

        public override int GetHashCode() {
            return HashCode.Combine(end, lane);
        }

        public static bool operator ==(LaneEnd left, LaneEnd right) {
            return left.Equals(right);
        }

        public static bool operator !=(LaneEnd left, LaneEnd right) {
            return !(left == right);
        }
    }

    public class Lane(RoadNode node): IDraggableObj {
        public RoadNode RoadNode => node; // Reference to the road node this lane belongs to
        private LaneSpec _spec;
        /// <summary>
        /// Specification of the lane, including properties like color, type, etc.
        /// The width here is ignored when set, but it's returned with the proper value when get.
        /// </summary>
        public LaneSpec Spec { get {
            _spec.Width = Width;
            return _spec;
        } set {
            if (value == _spec) return;
            _spec = value;
            RoadNode.InvalidateMesh();
            foreach(var connection in Connections) 
                connection.InvalidateMesh();
        }} 
        public float LeftPosition { get; set; } // Left position of the lane relative to the road node
        public float RightPosition { get; set; } // Right position of the lane relative to the road node
        public int Index { get; internal set; } // Index of the lane in the road node's lane list
        public float MiddlePosition => (LeftPosition + RightPosition) / 2; // Middle position of the lane, calculated as the average of left and right positions
        public float Width => RightPosition - LeftPosition;
        public LaneEnd Rear => new LaneEnd(NodeEnd.Backward, this);
        public LaneEnd Front => new LaneEnd(NodeEnd.Forward, this);

        //Positioning utilities
        public void Align(float t, float pos, float width = -1) {
            if (width < 0) width = Width;
            LeftPosition = pos - t * width;
            RightPosition = LeftPosition + width;
        }

        public LaneEnd GetEnd(NodeEnd end) {
            return end.GetConditional(Rear, Front);
        }

        //Indexing
        internal ISet<LaneStrip> connections = new HashSet<LaneStrip>(); // Set of lane strips that this lane is connected to
        public ISet<LaneStrip> Connections => new ReadOnlySet<LaneStrip>(connections); // Read-only set of lane strips that this lane is connected to

        //Dragging
        public void Drag(Vector3 vector) => ((IDraggableObj)RoadNode).Drag(vector);
    }
}
