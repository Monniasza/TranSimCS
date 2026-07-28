using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iesi.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Range;
using TranSimCS.Setting;
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
        public ImmutableDictionary<RoadDataBuilder.LaneConnectionData, LaneStripData> LaneConnections { get; private set; }

        //Caches
        private OrthodistantBasis? _splineFrame;
        public OrthodistantBasis OrthodistantBasis => _splineFrame ??= GenerateOrthodistantBasis();
        private OrthodistantBasis GenerateOrthodistantBasis() => IndexSpline.ToOrthodistantBasis(StartPos, EndPos);
        private DualRange? _bounds;
        public DualRange Bounds => _bounds ??= CalculateBounds();
        private DualRange CalculateBounds() {
            DualRange result = default;
            foreach(var strip in LaneConnections) 
                result |= strip.Value.Bounds;
            return result;
        }

        internal RoadStripData(RoadDataBuilder rdb) {
            Finish = rdb.RoadFinish;
            IndexSpline = rdb.IndexSpline;
            StartLanes = rdb.StartLanes;
            EndLanes = rdb.EndLanes;
            StartPos = rdb.StartPos;
            EndPos = rdb.EndPos;
            LaneConnections = rdb.LaneConnections
                .Select(x => new KeyValuePair<RoadDataBuilder.LaneConnectionData, LaneStripData>(x, new LaneStripData(this, x)))
                .ToImmutableDictionary();
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
        private bool? _isReverse;
        public bool IsReverse => _isReverse ??= Parent.StartLanes != Parent.EndLanes && Parent.StartLanes.Contains(EndNode);
        private OrthodistantLUT? _centerLUT;
        public OrthodistantLUT CenterLUT => _centerLUT ??= GenerateCenterLineLUT();
        private OrthodistantLUT? GenerateCenterLineLUT() {
            var startT = StartNode.CenterPos;
            var endT = EndNode.CenterPos;
            if(IsReverse) DataUtil.Swap(ref startT, ref endT);
            var points = Parent.OrthodistantBasis.Offset(startT, -endT);
            return new OrthodistantLUT(points);
        }
        private DualRange? _bounds;
        public DualRange Bounds => _bounds ??= GenerateBounds();
        private DualRange GenerateBounds() {
            var startLane = StartNode;
            var endLane = EndNode;
            if(IsReverse) DataUtil.Swap(ref startLane, ref endLane);
            return new(startLane.Bounds, endLane.Bounds);
        }

        
        private GridMesh<Vector3, RoadSplineComponent>? _allStrips;
        public GridMesh<Vector3, RoadSplineComponent> AllStrips => _allStrips ??= GenerateStripList();
        private GridMesh<Vector3, RoadSplineComponent> GenerateStripList() {
            //Accumulate components from listeners
            var generatedComponents = StripRenderer.GenerateStripSplineComponents(this);

            //Generate spline strips
            var vertcount = generatedComponents.Length * 2;
            var accuracy = Settings.RoadAccuracy;
            Vector3[,] vertices = new Vector3[vertcount, accuracy];

            var records = new GridCrossSectionalRecord<RoadSplineComponent>[generatedComponents.Length];

            //Generate a GridMesh
            for (int i = 0; i < vertcount; i += 2) {
                var j = i / 2;
                var component = generatedComponents[j];
                var surface = component.Item1;
                var range = component.Item2;
                var (lspline, rspline) = range.GenerateRoadSplineRange(Parent);
                for (int k = 0; k < accuracy; k++) {
                    vertices[i, k] = lspline[k];
                    vertices[i + 1, k] = rspline[k];
                }
                records[j] = new(i, i + 1, surface);
            }

            return new GridMesh<Vector3, RoadSplineComponent>(Immutable2DArray<Vector3>.Wrap(vertices), records.ToImmutableArray());
        }
    }
}
