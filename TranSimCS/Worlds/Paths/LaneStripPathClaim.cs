using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Spline;

namespace TranSimCS.Worlds.Paths {
    /// <summary>
    /// The <see cref="IPathClaim"/> generated for a <see cref="LaneStrip"/>.
    /// <para>
    /// The claim joins the strip's two attachment points into a single spline. The spline runs from the
    /// start attachment to the end attachment, in the direction of travel of the strip, so it is already
    /// reverse-corrected for strips that are driven against the direction of their road.
    /// </para>
    /// <para>
    /// The claim listens to both attachment points and raises <see cref="ObjectChanged"/> whenever either
    /// of them changes, which marks the owning path dirty so that its spline is regenerated lazily.
    /// </para>
    /// </summary>
    public sealed class LaneStripPathClaim : IPathClaim {
        /// <summary>
        /// The lane strip this claim was generated for.
        /// </summary>
        public LaneStrip Strip { get; }

        /// <inheritdoc/>
        public event Action? ObjectChanged;

        /// <inheritdoc/>
        /// <remarks>
        /// A <see cref="LaneStrip"/> is not an <see cref="Obj"/> itself, so the claim reports the road
        /// strip that contains it as its owning object.
        /// </remarks>
        public Obj Object => Strip.Road;

        /// <summary>
        /// Creates a claim for the given lane strip and its two attachment points.
        /// </summary>
        /// <param name="strip">The lane strip this claim belongs to.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any argument is <see langword="null"/>.
        /// </exception>
        public LaneStripPathClaim(LaneStrip strip) {
            ArgumentNullException.ThrowIfNull(strip, nameof(strip));
            Strip = strip;
            Strip.Changed += OnAttachmentChanged;
        }

        /// <summary>
        /// Detaches this claim from its attachment points.
        /// <para>
        /// Called when the owning path is collected, so that a collected path stops receiving change
        /// notifications from the road network.
        /// </para>
        /// </summary>
        public void Detach() {
            Strip.Changed -= OnAttachmentChanged;
        }

        /// <summary>
        /// Raises <see cref="ObjectChanged"/> when either attachment point changes.
        /// </summary>
        private void OnAttachmentChanged() => ObjectChanged?.Invoke();

        /// <summary>
        /// Generates the spline joining the strip's two attachment points.
        /// <para>
        /// The spline is built from the two attachment reference frames and their lateral offsets, using
        /// the strip's spline generator so that the generated path matches the geometry the strip is
        /// rendered with.
        /// </para>
        /// </summary>
        /// <returns>The generated spline basis, running from the start attachment to the end attachment.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when either attachment point is dead.
        /// </exception>
        public OrthodistantBasis GenerateSpline() {
            if (Strip.IsDead)
                throw new InvalidOperationException("Cannot generate a spline for a path whose attachment points are dead");
            if (Strip.IsReverse())
                return Strip.SplineLUT.spline.Reverse();
            return Strip.SplineLUT.spline;
        }

        /// <summary>
        /// Returns a string describing this claim, for diagnostics.
        /// </summary>
        public override string ToString() => $"LaneStripPathClaim({Strip.Guid})";
    }
}
