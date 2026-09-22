using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Geometry;
using TranSimCS.Roads.Node;
using TranSimCS.Spline;

namespace TranSimCS.Worlds.Paths {
    /// <summary>
    /// An attachment point backed by a road <see cref="HalfLane"/>.
    /// <para>
    /// This is the attachment point used by ordinary road traffic. The path centre line is placed at the
    /// middle of the lane, and the direction of travel is the direction the lane is driven in: a
    /// <see cref="NodeEnd.Forward"/> half lane is driven away from its node, a
    /// <see cref="NodeEnd.Backward"/> half lane is driven towards its node.
    /// </para>
    /// <para>
    /// The attachment point stays alive for as long as the lane it refers to is still part of a road
    /// node that is in a world. Deleting the road node, or removing the lane from it, makes the
    /// attachment point dead; the path it belongs to is then orphaned rather than deleted, so that
    /// traffic already on the path can leave it.
    /// </para>
    /// </summary>
    public sealed class HalfLaneAttachment : IPathAttachment {
        /// <summary>
        /// The half lane this attachment point is anchored to.
        /// </summary>
        public HalfLane HalfLane { get; }

        /// <summary>
        /// The lane this attachment point is anchored to.
        /// </summary>
        public Lane Lane => HalfLane.Lane;

        /// <summary>
        /// The road node this attachment point is anchored to.
        /// </summary>
        public RoadNode RoadNode => HalfLane.RoadNode;

        /// <summary>
        /// The end of the road node this attachment point is anchored to.
        /// </summary>
        public NodeEnd End => HalfLane.End;

        /// <inheritdoc/>
        public event Action? Changed;

        /// <inheritdoc/>
        public Obj Owner => RoadNode;

        /// <summary>
        /// Whether the lane this attachment point refers to is still part of its road node.
        /// <para>
        /// A lane is removed from its node when the node is demolished or when the lane is deleted in
        /// the node editor. Either way the attachment point becomes dead and the path must be orphaned.
        /// </para>
        /// <para>
        /// Removing a lane clears the lane's back-reference to its node, so the node is read through the
        /// lane rather than cached: a cached node would still look alive after the lane was removed.
        /// </para>
        /// </summary>
        public bool IsAlive {
            get {
                var node = Lane.RoadNode;
                if (node == null) return false;
                if (node.World == null) return false;
                return node.Lanes.Contains(Lane);
            }
        }

        /// <summary>
        /// The reference frame of the road node end this attachment point belongs to.
        /// <para>
        /// The frame is already oriented along the direction of travel, so a
        /// <see cref="NodeEnd.Backward"/> half lane yields a frame pointing away from the node.
        /// </para>
        /// </summary>
        public Transform3 ReferenceFrame => HalfLane.HalfNode.Cache.ReferenceFrame;

        /// <summary>
        /// The lateral offset of the lane centre line from the node reference frame origin, in metres.
        /// </summary>
        public float Offset => HalfLane.MiddlePosition;

        /// <summary>
        /// Creates an attachment point for the given half lane.
        /// </summary>
        /// <param name="halfLane">The half lane to anchor to.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="halfLane"/> is <see langword="null"/>.
        /// </exception>
        public HalfLaneAttachment(HalfLane halfLane) {
            ArgumentNullException.ThrowIfNull(halfLane, nameof(halfLane));
            HalfLane = halfLane;
            RoadNode.LaneRemoved += RoadNode_LaneRemoved;
            RoadNode.PositionProp.ValueChanged += PositionProp_ValueChanged;
        }

        /// <summary>
        /// Detaches this attachment point from the road node it listens to.
        /// <para>
        /// Called when the path that owns this attachment point is collected, so that a collected path
        /// does not keep a road node alive or keep receiving change notifications.
        /// </para>
        /// </summary>
        public void Detach() {
            RoadNode.LaneRemoved -= RoadNode_LaneRemoved;
            RoadNode.PositionProp.ValueChanged -= PositionProp_ValueChanged;
        }

        /// <summary>
        /// Raises <see cref="Changed"/> when the lane this attachment point refers to is removed from
        /// its road node, so that the owning path can be orphaned before the lane is destroyed.
        /// </summary>
        private void RoadNode_LaneRemoved(object? sender, RoadNode.LaneEventArgs e) {
            if (e.lane != Lane) return;
            Changed?.Invoke();
        }

        /// <summary>
        /// Raises <see cref="Changed"/> when the road node is moved, so that the owning path
        /// regenerates its spline.
        /// </summary>
        private void PositionProp_ValueChanged(object? sender, PositionEulerAngles oldValue, PositionEulerAngles newValue) {
            Changed?.Invoke();
        }

        /// <summary>
        /// Returns a string describing this attachment point, for diagnostics.
        /// </summary>
        public override string ToString() => $"HalfLaneAttachment({Lane.Guid}, {End})";
    }
}
