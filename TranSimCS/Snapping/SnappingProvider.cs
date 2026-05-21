using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TranSimCS.Worlds;

namespace TranSimCS.Snapping {
    public interface ISnappingProvider {
        PositionSnap? Snap(
            PositionSnapContext context
        );
        public string Name { get; }
    }

    public sealed record PositionSnap(
        Vector3 Position,
        float Score,
        string Name
    );

    public sealed class PositionSnapContext {
        public required Vector3 StartPosition;
        public required Vector3 StartTangent;

        public required TSWorld World;

        public required KeyboardState Keyboard;
    }

    public sealed record GridDefinition(
        Vector3 Origin,
        float Azimuth,
        float CellSize,
        float MinX,
        float MinZ,
        float MaxX,
        float MaxZ
    );

    public sealed class GridSnap : ISnappingProvider {
        public GridDefinition Grid;
        public Rectangle GridDimensions;

        public string Name => "Snap to grid";

        public PositionSnap? Snap(PositionSnapContext context) {


            throw new NotImplementedException();
        }

        public static Vector3 SnapToGrid(
            Vector3 point,
            GridDefinition grid
        ) {
            if (grid.CellSize == 0) return point;

            Vector3 axisX = new(
                MathF.Cos(grid.Azimuth),
                0,
                MathF.Sin(grid.Azimuth)
            );

            Vector3 axisZ = new(
                -axisX.Z,
                0,
                axisX.X
            );

            Vector3 delta =
                point - grid.Origin;

            float localX =
                Vector3.Dot(delta, axisX);

            float localZ =
                Vector3.Dot(delta, axisZ);

            if (localX < grid.MinX
            || localX > grid.MaxX
            || localZ < grid.MinZ
            || localZ > grid.MaxZ) {
                return point;
            }

            float snappedX =
                MathF.Round(
                    localX / grid.CellSize
                ) * grid.CellSize;

            float snappedZ =
                MathF.Round(
                    localZ / grid.CellSize
                ) * grid.CellSize;

            Vector3 result = grid.Origin
                + axisX * snappedX
                + axisZ * snappedZ;
            result.Y = point.Y;
            return result;
        }
    }
}
