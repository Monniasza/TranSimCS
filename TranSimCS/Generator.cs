using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS
{
    internal static class Generator
    {
        public static void GenerateLanes(int count, RoadNode node, float laneWidth = 3.5f, float offset = 0)
        {
            if (count < 1)
            {
                throw new ArgumentException("Count must be at least 1.", nameof(count));
            }
            // Clear existing position offsets
            node.PositionOffsets.Clear();
            // Generate lane positions based on the count and lane width
            for (int i = 0; i <= count; i++)
            {
                float pos = i * laneWidth + offset;
                node.PositionOffsets.Add(pos);
            }
            //Generate lane specifications for each lane
            for (int i = 0; i < count; i++)
            {
                var laneSpec = new LaneSpec
                {
                    Color = Color.Gray, // Default color, can be customized
                    VehicleTypes = VehicleTypes.All // Default to all vehicle types, can be customized
                };
                node.LaneSpecs.Add(laneSpec);
            }
        }
    }
}
