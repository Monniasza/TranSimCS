using System;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.StripData {
    public sealed class RoadDataBuilder {
        //Per-road properties
        public RoadFinish RoadFinish { get; set; } = RoadFinish.Embankment;
        private NodeSpec? _startLanes;
        public NodeSpec StartLanes {
            get => _startLanes ?? throw new InvalidOperationException("StartLanes not initialized");
            set => _startLanes = value;
        }
        private NodeSpec? _endLanes;
        public NodeSpec EndLanes {
            get => _endLanes ?? throw new InvalidOperationException("EndLanes not initialized");
            set => _endLanes = value;
        }
        private IndexSpline? _indexSpline;
        public IndexSpline IndexSpline {
            get => _indexSpline ?? throw new InvalidOperationException("IndexSpline not initialized");
            set => _indexSpline = value;
        }
        private PositionEulerAngles? _startPos;
        public PositionEulerAngles StartPos {
            get => _startPos ?? throw new InvalidOperationException("StartPos not initialized");
            set => _startPos = value;
        }
        private PositionEulerAngles? _endPos;
        public PositionEulerAngles EndPos {
            get => _endPos ?? throw new InvalidOperationException("EndPos not initialized");
            set => _endPos = value;
        }
        public struct LaneConnectionData : IEquatable<LaneConnectionData> {
            public LaneSpec LaneSpec = LaneSpec.Default;
            public LaneNode StartNode;
            public LaneNode EndNode;
            public object? Tag;

            public LaneConnectionData() { }
            public LaneConnectionData(LaneSpec laneSpec, LaneNode startNode, LaneNode endNode, object? tag = null) {
                LaneSpec = laneSpec;
                StartNode = startNode;
                EndNode = endNode;
                Tag = tag;
            }

            public override bool Equals(object? obj) {
                return obj is LaneConnectionData data && Equals(data);
            }

            public bool Equals(LaneConnectionData other) {
                return LaneSpec.Equals(other.LaneSpec) &&
                       EqualityComparer<LaneNode>.Default.Equals(StartNode, other.StartNode) &&
                       EqualityComparer<LaneNode>.Default.Equals(EndNode, other.EndNode) &&
                       EqualityComparer<object?>.Default.Equals(Tag, other.Tag);
            }

            public override int GetHashCode() {
                return HashCode.Combine(LaneSpec, StartNode, EndNode, Tag);
            }

            public static bool operator ==(LaneConnectionData left, LaneConnectionData right) {
                return left.Equals(right);
            }

            public static bool operator !=(LaneConnectionData left, LaneConnectionData right) {
                return !(left == right);
            }
        }

        private HashSet<LaneConnectionData> data = new();
        public ReadOnlySet<LaneConnectionData> LaneConnections => new(data);
        public void AddConnection(LaneConnectionData connection) {
            ArgumentNullException.ThrowIfNull(connection.StartNode, "connection.StartNode");
            ArgumentNullException.ThrowIfNull(connection.EndNode, "connection.EndNode");
            if (data.Contains(connection)) throw new ArgumentException("Connection already exists");
            bool isContained = StartLanes.Contains(connection.StartNode) ? EndLanes.Contains(connection.EndNode)
                : StartLanes.Contains(connection.EndNode) && EndLanes.Contains(connection.StartNode);
            if (!isContained) throw new ArgumentException("Connection is not contained within lane set. Try setting lanes first.");
            data.Add(connection);
        }
        public void AddConnection(LaneNode start, LaneNode end, LaneSpec? laneSpec = null, object? tag = null)
            => AddConnection(new(laneSpec ?? LaneSpec.Default, start, end, tag));

        public RoadStripData Create() => new RoadStripData(this);
    }
}
