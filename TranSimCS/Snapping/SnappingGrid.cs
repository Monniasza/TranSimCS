using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.SceneGraph;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Snapping {
    public class SnappingGrid : Obj, IPosition, IObjMesh<SnappingGrid> {
        public Property<ObjPos> PositionProp { get; private set; }
        public readonly Property<float> CellSizeProp;
        public readonly Property<uint> CellCountProp;
        public float CellSize { get => CellSizeProp.Value; set => CellSizeProp.Value = value; }
        public uint CellCount { get => CellCountProp.Value; set => CellCountProp.Value = value; }
        public ObjPos Position { get => PositionProp.Value; set => PositionProp.Value = value; }

        public MeshGenerator<SnappingGrid> Mesh { get; private set; }

        public SnappingGrid() {
            PositionProp = new(new(new(0, 0.1f, 0), 0), "pos", this);
            CellSizeProp = new(4, "cellSize", this);
            CellCountProp = new(20, "cellCount", this);
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
    }
}
