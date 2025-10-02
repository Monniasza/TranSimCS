using System;
using Microsoft.Xna.Framework;

namespace TranSimCS.Worlds {
    /// <summary>
    /// Interface for objects that have a position in 3D space.
    /// </summary>
    public interface IPosition: IDraggableObj {
        /// <summary>
        /// The position property of the object.
        /// </summary>
        Property<ObjPos> PositionProp { get; }
        public ObjPos PositionData { get => PositionProp.Value; set => PositionProp.Value = value; }

        void IDraggableObj.Drag(Vector3 vector, Vector3 dragFrom) {
            var posdata = PositionData;
            posdata.Position += vector;
            PositionData = posdata;
        }
    }
}
