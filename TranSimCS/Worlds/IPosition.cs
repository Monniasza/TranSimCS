using System;
using Microsoft.Xna.Framework;
using TranSimCS.Worlds.Property;

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

        void IDraggableObj.Rotate(int fieldAzimuth, float pitch, float tilt) {
            var pos = PositionData;
            pos.Azimuth += fieldAzimuth;
            pos.Inclination += pitch;
            pos.Tilt += tilt;
            PositionData = pos;
        }
    }
}
