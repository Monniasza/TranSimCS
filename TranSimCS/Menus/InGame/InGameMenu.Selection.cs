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

        //In-world UI
        public MultiMesh SelectorObjects { get; private set; }
        public MultiMesh InvisibleSelectors { get; private set; }

        private void CreateSelectors() {
            // CRITICAL: Clear SelectorObjects and InvisibleSelectors to prevent geometry accumulation across frames
            // Without this, meshes from previous frames accumulate causing Z-fighting and flickering
            SelectorObjects.Clear();            
            InvisibleSelectors.Clear();
            
            if (CheckSegments.Checked)
                foreach (var road in World.RoadSegments)
                    InvisibleSelectors.AddAll(road.Mesh.GetMesh());
            if (CheckNodes.Checked)
                foreach (var node in World.RoadNodes)
                    SelectorObjects.AddAll(node.Mesh.GetMesh());
            if (CheckSections.Checked)
                foreach (var section in World.RoadSections)
                    InvisibleSelectors.AddAll(section.Mesh.GetMesh());

            //Add tool selectors for collision detection
            configuration.Tool?.AddSelectors(InvisibleSelectors, SelectorObjects);

            InvisibleSelectors.AddAll(SelectorObjects);
        }
    }
}
