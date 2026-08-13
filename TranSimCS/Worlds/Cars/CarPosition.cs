using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Strip;
using TranSimCS.Save2.TypeRegistry;

namespace TranSimCS.Worlds.Cars {
    public abstract class CarPosition: ITypeRegistered<CarPosition> {
        /// <summary>
        /// Returns a new <see cref="CarPosition"/> with its position increased by <paramref name="amount"/>.
        /// </summary>
        /// <exception cref="ArgumentException">if <paramref name="amount"/> is not a finite real number</exception>
        public abstract CarPosition? Advance(float amount);
        /// <summary>
        /// Returns all legal transitions from the given endpoint of the current state, all with initial position of 0
        /// </summary>
        [Obsolete("Used for existing road transitions only. Planned conversion to spatial-index-based road transitions")]
        public abstract IEnumerable<CarPosition> FindNext(SegmentHalf end);
        /// <summary>
        /// Returns current arc-length position of the current state
        /// </summary>
        public abstract float CurrentPosition();
        /// <summary>
        /// Returns the arc-length of the underlying road of the current state
        /// </summary>
        public abstract float MaxPosition();
        /// <summary>
        /// Returns an arc-length lookup of (x, y, z, t) positions where (x, y, z) is the approximate position
        /// and t is the inteprolation parameter for <see cref="GetPositionFrame(float)"/>
        /// </summary>
        public abstract LUT GetPositionLookup();
        /// <summary>
        /// Returns a more accurate position frame along with (right, normal, tangent) basis vectors
        /// </summary>
        public abstract Transform3 GetPositionFrame(float t);
        /// <summary>
        /// Returns the stable type name of this state for serialization
        /// </summary>
        public abstract string TypeName();
        (string TypeId, TypeRegistry<CarPosition> TypeRegistry) ITypeRegistered<CarPosition>.TypeInfo() => (TypeName(), Car.CarPositionRegistry);
    }
}
