using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.ModelOld;

namespace TranSimCS.Roads.Node {
    public static class NodeRenderer {
        public static void GenerateRoadNodeSelectionMesh(RoadNode node, Mesh mesh, LaneEnd? SelectedLaneEnd) {
            Mesh roadRenderBin = mesh;
            var refframe = node.ReferenceFrame;
            foreach (var lane in node.Lanes) {
                foreach (var laneEnd in new LaneEnd[] { lane.Front, lane.Rear }) {
                    var altColor = lane.Spec.Color;
                    altColor.A /= 2;
                    var color = (SelectedLaneEnd == laneEnd) ? InGameMenu.laneHighlightColor : (SelectedLaneEnd == null || !node.Lanes.Contains(SelectedLaneEnd.Value.lane)) ? altColor : InGameMenu.roadSegmentHighlightColor;
                    var range = lane.Bounds;
                    var zdiscriminant = laneEnd.end.GetConditional(-1, 0);
                    var quad = GenerateLaneQuad(node, range.Min, range.Max, color, 0.2f, zdiscriminant, zdiscriminant+1);
                    roadRenderBin.DrawQuad(quad);
                    roadRenderBin.AddTagsToLastTriangles(2, laneEnd);
                }
            }
        }

        public static void CreateAddLanes(RoadNode nodeEnd, Mesh mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            if (nodeEnd.Lanes.Count < 1) return;
            var bounds = nodeEnd.Bounds;
            var leftLimit = bounds.Min;
            var rightLimit = bounds.Max;
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.FrontEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(-1, leftLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
            CreateAddLane(new AddLaneSelection(1, rightLimit, nodeEnd.RearEnd), mesh, size, color, voffset);
        }
        public static QuadOld CreateAddLane(AddLaneSelection als, Mesh mesh, float size = 1, Color? color = null, float voffset = 0.2f) {
            var zrange = GeometryUtils.RoadEndToRange(als.nodeEnd.End) * size;
            var xrange = als.CalculateOffsets(size);
            QuadOld quad = GenerateLaneQuad(als.nodeEnd.Node, xrange.Min, xrange.Max, color ?? Colors.SemiClearGray, voffset, zrange.X, zrange.Y);
            mesh.DrawQuad(quad);
            mesh.AddTagsToLastTriangles(2, als);
            return quad;
        }

        public static QuadOld GenerateNodeQuad(RoadNode node, Color color, float voffset = 0.2f, float minZ = -1, float maxZ = 1) {
            var range = node.Bounds;
            return GenerateLaneQuad(node, range.Min, range.Max, color, voffset, minZ, maxZ);
        }
        public static QuadOld GenerateLaneQuad(Lane lane, Color? color, float voffset = 0.2f, float minZ = -1, float maxZ = 1) {
            var range = lane.Bounds;
            var altColor = lane.Spec.Color;
            altColor.A /= 2;
            return GenerateLaneQuad(lane.RoadNode, range.Min, range.Max, color ?? altColor, voffset, minZ, maxZ);
        }
        public static QuadOld GenerateLaneQuad(RoadNode node, float lb, float rb, Color color, float voffset = 0.2f, float minZ = -1, float maxZ = 1) {
            Vector3 offset = new(0, voffset, 0);
            Transform3 transform = node.PositionProp.Value.CalcReferenceFrame();
            var vl = transform.O + lb * transform.X;
            var vr = transform.O + rb * transform.X;
            var vd = vl + transform.Z * minZ;
            var vc = vr + transform.Z * minZ;
            var va = vl + transform.Z * maxZ;
            var vb = vr + transform.Z * maxZ;
            return new QuadOld(va, vb, vc, vd, color) + offset;
        }

        public static void GenerateNodeVisualMesh(RoadNode node, MultiMesh mesh) {
            foreach(var lane in node.Lanes) {
                GenerateLaneMesh(lane, mesh);
            }
        }
        public static void GenerateLaneMesh(Lane lane, MultiMesh mesh, float voffset = 0) {
            //Generate the stop/yield line
            var tags = lane.Spec.Flags;
            var lineFlags = LaneFlags.Stop | LaneFlags.Yield;
            var lineTest = tags & lineFlags;
            if (lineTest != 0) {
                var lineBin = mesh.GetOrCreateRenderBinForced((lineTest == LaneFlags.Yield) ? Assets.LineYield : Assets.Road);
                var range = lane.Bounds;
                var width = range.Max - range.Min;
                var refframe = lane.RoadNode.ReferenceFrame;
                var p0 = refframe.O + refframe.X * range.Min;
                var p1 = refframe.O + refframe.X * range.Max;
                lineBin.DrawLine(p0, p1, refframe.Y, Color.White, length: width);
            }
        }
    }
}
