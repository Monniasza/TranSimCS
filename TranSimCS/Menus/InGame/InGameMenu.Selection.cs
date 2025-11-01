using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;

namespace TranSimCS.Menus.InGame {
    //Selection-related stuff
    public partial class InGameMenu {
        public RoadSelection? MouseOverRoad { get; set; } = null; // Store the selected road selection
        public object? SelectedObject = null;
        public Vector3 IntersectWithGround(Ray ray) {
            return GeometryUtils.IntersectRayPlane(ray, groundPlane);
        }
        public Vector3 GroundSelection => IntersectWithGround(MouseRay);
        public Vector3 GroundSelectionOld => IntersectWithGround(MouseRayOld);

        public MultiMesh SelectorObjects { get; private set; }

        private void CreateSelectors() {
            // CRITICAL: Clear SelectorObjects and InvisibleSelectors to prevent geometry accumulation across frames
            // Without this, meshes from previous frames accumulate causing Z-fighting and flickering
            SelectorObjects.Clear();

            World.SectionsGraph.Active = CheckSections.Checked;
            World.RoadSegments.trackerSpatial.sceneTree.Active = CheckSegments.Checked;
            World.Nodes.tracker.sceneTree.Active = CheckNodes.Checked;

            //Add tool selectors for collision detection
            var tempSelectors = new MultiMesh();
            configuration.Tool?.AddSelectors(tempSelectors, SelectorObjects);

            tempSelectors.AddAll(SelectorObjects);

            World.TempSelectorsMesh.Value = tempSelectors;
        }
    }
}
