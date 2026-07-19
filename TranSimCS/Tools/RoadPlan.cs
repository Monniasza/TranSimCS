using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class RoadPlan {
        public Vector3 startTangent;
        public Vector3 startPos;
        public Vector3 startLateral;

        public Vector3 endTangent;
        public Vector3 endPos;
        public Vector3 endLateral;

        public InGameMenu menu;

        public void Align(Alignment alignment, float width) {
            var calculatedAlignments = alignment.GetAlignments();
            var moveRight = calculatedAlignments.r - 0.5f;
            startPos += moveRight * startLateral * width;
            endPos += moveRight * endLateral * width;
        }
    }
}
