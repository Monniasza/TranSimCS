using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS
{
    internal static class Geometry
    {
        /// <summary>
        /// Calculates the distance between two points in 2D space.
        /// </summary>
        /// <param name="x1">X coordinate of the first point.</param>
        /// <param name="y1">Y coordinate of the first point.</param>
        /// <param name="x2">X coordinate of the second point.</param>
        /// <param name="y2">Y coordinate of the second point.</param>
        /// <returns>The distance between the two points.</returns>
        public static float Distance(float x1, float y1, float x2, float y2)
        {
            return MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        /// <summary>
        /// Calculates the end position of a line segment given a starting position, an offset, and an angle.
        /// </summary>
        /// <param name="nodePos">road node position</param>
        /// <param name="offset">Offset from the centerline</param>
        /// <param name="angle">Angle in the 2^32 field</param>
        /// <returns></returns>
        public static Vector3 calcLineEnd(Vector3 nodePos, float offset, int angle)
        {
            float radians = (angle / (float)(1 << 32)) * MathF.PI * 2; // Convert angle to radians
            float x = nodePos.X + offset * MathF.Cos(radians);
            float z = nodePos.Z + offset * MathF.Sin(radians); // Use Z for the vertical axis in 3D space
            return new Vector3(x, nodePos.Y, z); // Return the end position as a Vector3
        }
    }
}
