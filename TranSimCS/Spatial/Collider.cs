using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace TranSimCS.Spatial {

    //Objects that can be intersection-checked with bounding boxes
    public interface Collider{
        public bool Intersects(BoundingBox box);
        public bool Contains(BoundingBox box);
    }
}
