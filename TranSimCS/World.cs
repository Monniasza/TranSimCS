using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using System.Collections.ObjectModel;

namespace TranSimCS
{
    public class World
    {
        public ObservableCollection<LaneConnection> RoadSegments { get; } = new();
        public ObservableCollection<RoadNode> RoadNodes { get; } = new();


        public void Update(float deltaTime)
        {

            // Update logic for the world can be added here
            foreach (var node in RoadNodes)
            {
                // Example: Update each road node's position or state
                
            }
        }
    }
}
