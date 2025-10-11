using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Model;
using TranSimCS.Roads;

namespace TranSimCS.Geometry {
    public class SplineFrameCoverter {
        /* For the SplineFrame it is true that splineFrame(a+x, b+y)[z] = splineFrame(a-x, b-y)[z].
         * This can be exploited to find an intersection.
         * To do the intersection, set a and b equal
         * 
         */

        public readonly WorkingPlane plane;
        public readonly Mesh lookupMesh;
        private SplineFrameCoverter(WorkingPlane plane, Mesh lookupMesh) {
            this.plane = plane;
            this.lookupMesh = lookupMesh;
        }

        public static SplineFrameCoverter ConstructFrom(RoadStrip strip) {
            throw new NotImplementedException();
        }
    }
}
