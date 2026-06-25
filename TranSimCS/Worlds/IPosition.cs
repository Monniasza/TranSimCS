using System;
using Microsoft.Xna.Framework;
using TranSimCS.Property;

namespace TranSimCS.Worlds {
    /// <summary>
    /// Interface for objects that have a position in 3D space.
    /// </summary>
    public interface IPosition: IDraggableObj {
        /// <summary>
        /// The position property of the object.
        /// </summary>
        Property<ObjPos> PositionProp { get; }
        public ObjPos PositionData { get => PositionProp.Value; set => PositionProp.Value = value;}
        IPosition[] IDraggableObj.DraggableComponents() => [this];
    }

    /// <summary>
    /// Adapts a Property&lt;ObjPos&gt; to IPosition.
    /// </summary>
    /// <param name="property"></param>
    public class PositionAdapter(Property<ObjPos> property) : IPosition {
        public Property<ObjPos> PositionProp => property;
    }
}
