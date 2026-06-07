using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TranSimCS.Property;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Tools;

namespace TranSimCS.Tools {
    public class LaneEditorTool : ITool {
        private RoadNodeEnd _selectedNodeEnd;
        private List<LaneBoundHandle> _handles = new();

        public void OnActivate() {
            // Logic to detect selection via InGameMenu.SelectorObjects 
            // and populate _handles with LaneBoundHandles for each lane in the node.
        }

        public void OnUpdate() {
            foreach (var handle in _handles) {
                handle.OnDrag(Mouse.GetDelta()); // Simplified logic
            }
        }

        public void OnDeactivate() {
            _handles.Clear();
        }

        public void Render() {
            // Draw the selection UI and labels for each lane bound
        }

        public void OnMouseDown(Vector2 mousePos) {
            foreach (var handle in _handles) {
                if (Vector3.Distance(handle.Position, new Vector3(mousePos.X, mousePos.Y, 0)) < 10f) {
                    handle.OnClick();
                }
            }
        }

        public void OnMouseMove(Vector2 mouseDelta) {
            // Pass delta to handles
        }
    }
}
