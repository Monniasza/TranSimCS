using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Tools;

namespace TranSimCS.Tools {
    /// <summary>
    /// A draggable handle that allows users to visually manipulate 
    /// the bounds of an individual lane within a road node.
    /// </summary>
    public class LaneBoundHandle : IDraggableObj {
        public readonly LaneLaneEnd ParentLane;
        public Vector3 Position; // The current visual position of the handle
        public float? CurrentValue; // The numerical value being edited (e.g., a bound offset)

        public LaneBoundHandle(LaneLaneEnd lane, float initialValue) {
            ParentLane = lane;
            CurrentValue = initialValue;
            // Position is initially set to the center of the lane's bounds
            Position = new Vector3(lane.Bounds.Center(); 0);
        }

        public bool IsSelected => true; // Handlers are usually selected by default when their parent tool is active

        public void OnDrag(Vector3 delta) {
            // Update the underlying lane data based on drag movement
            // This assumes a simple X-axis translation for local bounds
            float newVal = ParentLane.Bounds.Max + delta.X; 
            ParentLane.UpdateBound(newVal);
        }

        public void OnClick() {
            // Trigger the InGameMenu to show the numerical input for this specific lane
        }

        public Vector3 GetPosition() => Position;
    }
}
