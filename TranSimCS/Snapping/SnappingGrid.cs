using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Property;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;

namespace TranSimCS.Snapping {
    public class SnappingGrid : Obj, IPosition, IObjMesh<SnappingGrid> {
        public Property<ObjPos> PositionProp { get; private set; }
        /// <summary>
        /// Size of each snapping cell in meters
        /// </summary>
        public readonly Property<float> CellSizeProp;
        /// <summary>
        /// Number of cells in the snapping grid
        /// </summary>
        public readonly Property<uint> CellCountProp;
        /// <summary>
        /// If true, height reference will be from the grid, along its normal vector
        /// If false, height reference will from the ground, along the Y axis
        /// </summary>
        public readonly Property<bool> IsYLocalProp;
        /// <summary>
        /// Is the snapping range infinite? The rendered grid will still be finite.
        /// </summary>
        public readonly Property<bool> IsInfiniteProp;

        public float CellSize { get => CellSizeProp.Value; set => CellSizeProp.Value = value; }
        public uint CellCount { get => CellCountProp.Value; set => CellCountProp.Value = value; }
        public ObjPos Position { get => PositionProp.Value; set => PositionProp.Value = value; }
        public bool IsYLocal { get => IsYLocalProp.Value; set => IsYLocalProp.Value = value; }
        public bool IsInfinite { get => IsInfiniteProp.Value; set => IsInfiniteProp.Value = value; }

        public MeshGenerator<SnappingGrid> Mesh { get; private set; }

        public SnappingGrid() {
            PositionProp = new(new(new(0, 0.1f, 0), 0), "pos", this);
            CellSizeProp = new(4, "cellSize", this);
            CellCountProp = new(20, "cellCount", this);
            IsYLocalProp = new(true, "yLocal", this);
            IsInfiniteProp = new(false, "infinite", this);
            Mesh = new(this, GenerateMesh);
        }

        private void GenerateMesh(SnappingGrid grid, MultiMesh mesh) {
            float scale = CellCount;
            float increment = CellSize;
            float totalSize = scale * increment;
            ObjPos snapPos = Position;
            var refFrame = snapPos.CalcReferenceFrame();
            Mesh gridRenderBin = mesh.GetOrCreateRenderBinForced(Assets.Grid);
            Vector3 origin = refFrame.O - totalSize * refFrame.X - totalSize * refFrame.Z;
            gridRenderBin.DrawParallelogram(origin, refFrame.X * totalSize * 2, refFrame.Z * totalSize * 2, Color.White, new(-scale, -scale, 2 * scale, 2 * scale));
        }

        public Vector3 Snap(Vector3 input) {
            if(CellSize == 0) return input;

            var splineFrame = Position.CalcReferenceFrame();
            var lateral = splineFrame.X;
            var normal = splineFrame.Y;
            var tangent = splineFrame.Z;
            var center = splineFrame.O;

            if (!IsYLocal) {
                var oldNormal = normal;
                lateral = lateral.ToX0Z().Normalized();
                tangent = tangent.ToX0Z().Normalized();
                normal = Vector3.UnitY;
                if(MathF.Abs(oldNormal.Y) < 0.001f){
                    //Grid too close to vertical
                    return input;
                }
                center = center.ToX0Z();
            }

            var inRespectToCenter = input - splineFrame.O;
            var xComp = Vector3.Dot(inRespectToCenter, splineFrame.X);
            var yComp = Vector3.Dot(inRespectToCenter, splineFrame.Y);
            var zComp = Vector3.Dot(inRespectToCenter, splineFrame.Z);

            var totalSize = CellCount * CellSize;

            var roundedX = MathF.Round(xComp / CellSize);
            var roundedZ = MathF.Round(zComp / CellSize);
            var outOfRange = !IsInfinite && (Math.Abs(xComp) > totalSize || Math.Abs(zComp) > totalSize);
            if (!outOfRange) {
                //In range, snap the values
                xComp = roundedX * CellSize;
                zComp = roundedZ * CellSize;
            }

            return center + lateral * xComp + normal * yComp + tangent * zComp;
        }

        public Plane CreateSnappingPlane(float y = 0) {
            var refFrame = Position.CalcReferenceFrame();
            var refPlane = new Plane(refFrame.O + refFrame.Y * y, refFrame.Y);
            return refPlane;
        }
    }
}
