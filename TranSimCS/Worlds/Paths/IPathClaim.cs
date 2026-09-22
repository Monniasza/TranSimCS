using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;

namespace TranSimCS.Worlds.Paths {
    /// <summary>
    /// Represents a path claim - a combination of an update event, an object and a source of paths.
    /// </summary>
    public interface IPathClaim {
        /// <summary>
        /// Fire this event when the claimant changes in any way.
        /// </summary>
        public event Action? ObjectChanged;
        /// <summary>
        /// The claimant object. Multiple claims per object are allowed.
        /// </summary>
        public Obj Object { get; }
        /// <summary>
        /// Gets the associated spline for the object. The spline must be reverse-corrected.
        /// </summary>
        public OrthodistantBasis GenerateSpline();
        public bool IsAlive();
        public void Detach();
        /// <summary>
        /// The starting attachment of this claim
        /// </summary>
        public HalfLane? Start { get; }
        /// <summary>
        /// The ending attachment of this claim
        /// </summary>
        public HalfLane? End { get; }
    }
}
