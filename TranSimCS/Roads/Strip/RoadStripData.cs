using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Spline;
using TranSimCS.Worlds;

namespace TranSimCS.Roads.Strip {
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
        public struct LaneConnectionData{
            public LaneSpec LaneSpec = LaneSpec.Default;
            public LaneNode StartNode;
            public LaneNode EndNode;
            public LaneConnectionData() { }
            public LaneConnectionData(LaneSpec laneSpec, LaneNode startNode, LaneNode endNode) {
                LaneSpec = laneSpec;
                StartNode = startNode;
                EndNode = endNode;
            }
        }

        private List<LaneConnectionData> data;
        public ReadOnlyCollection<LaneConnectionData> LaneConnections => new(data);
        public void AddConnection(LaneConnectionData connection) {
            ArgumentNullException.ThrowIfNull(connection.StartNode, "connection.StartNode");
            ArgumentNullException.ThrowIfNull(connection.EndNode, "connection.EndNode");
            bool isContained = StartLanes.Contains(connection.StartNode) ? EndLanes.Contains(connection.EndNode)
                : StartLanes.Contains(connection.EndNode) && EndLanes.Contains(connection.StartNode);
            if (!isContained) throw new ArgumentException("Connection is not contained within lane set. Try setting lanes first.");
            data.Add(connection);
        }
        public void AddConnection(LaneNode start, LaneNode end, LaneSpec? laneSpec = null)
            => AddConnection(new(laneSpec ?? LaneSpec.Default, start, end));

        public RoadStripData Create() => new RoadStripData(this);
    }

    public sealed class RoadStripData {
        

        public RoadFinish Finish {get; private set; }
        public IndexSpline IndexSpline { get; private set; }
        public NodeSpec StartLanes { get; private set; }
        public NodeSpec EndLanes { get; private set; }
        public PositionEulerAngles StartPos {  get; private set; }
        public PositionEulerAngles EndPos { get; private set; }
        public ImmutableArray<LaneStripData> LaneConnections { get; private set; }

        //Caches
        private OrthodistantBasis? _splineFrame;
        public OrthodistantBasis OrthodistantBasis => _splineFrame ??= GenerateOrthodistantBasis();

        internal RoadStripData(RoadDataBuilder rdb) {
            Finish = rdb.RoadFinish;
            IndexSpline = rdb.IndexSpline;
            StartLanes = rdb.StartLanes;
            EndLanes = rdb.EndLanes;
            StartPos = rdb.StartPos;
            EndPos = rdb.EndPos;
            LaneConnections = rdb.LaneConnections.Select(x => new LaneStripData(this, x)).ToImmutableArray();
        }
    }

    public sealed class LaneStripData {
        public RoadStripData Parent { get; private set; }
        public LaneSpec Spec { get; private set; }
        public LaneNode StartNode { get; private set; }
        public LaneNode EndNode { get; private set; }
        internal LaneStripData(RoadStripData parent, RoadDataBuilder.LaneConnectionData lcd) {
            Parent = parent;
            Spec = lcd.LaneSpec;
            StartNode = lcd.StartNode;
            EndNode = lcd.EndNode;
        }

        //Caches
        private OrthodistantLUT? _centerLUT;
        public OrthodistantLUT CenterLUT => _centerLUT ??= GenerateCenterLineLUT();

        private GridMesh<Vector3, RoadSplineComponent>? _allStrips;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _allStrips ??= GenerateStripList();
    }
}
