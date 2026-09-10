using TranSimCS.Property;

namespace TranSimCS.Worlds {
    /// <summary>
    /// Interface for objects that have a position in 3D space.
    /// </summary>
    public interface IPosition: IDraggableObj {
        /// <summary>
        /// The object's position
        /// </summary>
        public PositionEulerAngles PositionData { get; set; }
        IPosition[] IDraggableObj.DraggableComponents() => [this];
    }
}
