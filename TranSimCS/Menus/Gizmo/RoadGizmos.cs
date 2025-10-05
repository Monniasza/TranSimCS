using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Model;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.Gizmo {
    public class TiltGizmo : IDraggableObj {
        public readonly RoadNode roadNode;
        // Creates a tilt gizmo tied to the provided road node instance.
        public TiltGizmo(RoadNode roadNode) {
            this.roadNode = roadNode;
        }

        // Raises an exception until tilt dragging is implemented.
        public void Drag(Vector3 vector, Vector3 dragFrom) {
            throw new NotImplementedException();
        }
        // Supplies the plane used to constrain dragging interactions for tilt adjustments.
        Plane IDraggableObj.DragPlane() {
            var frame = roadNode.PositionProp.Value.CalcReferenceFrame();
            var vert = frame.Y;
            var pos = frame.Transform(new Vector3(0, 10, 0));
            Plane plane = new(pos, vert);
            return plane;
        }
    }

    public class AzimuthGizmo(RoadNode roadNode) : IDraggableObj {
        // Rotates the associated road node based on the drag vector.
        public void Drag(Vector3 vector, Vector3 dragFrom) {
            var newPoint = vector + dragFrom;
            var oldRadius = dragFrom - roadNode.CenterPosition;
            var oldAzimuth = MathF.Atan2(oldRadius.X, oldRadius.Z);
            var newRadius = newPoint - roadNode.CenterPosition;
            var newAzimuth = MathF.Atan2(newRadius.X, newRadius.Z);
            var rotation = newAzimuth - oldAzimuth;
            var fieldRotation = GeometryUtils.RadiansToField(rotation);
            var frame = roadNode.PositionProp.Value;
            frame.Azimuth += fieldRotation;
            roadNode.PositionProp.Value = frame;
        }

        // Provides a horizontal plane anchored at the node for azimuth dragging.
        Plane IDraggableObj.DragPlane() {
            var pos = roadNode.PositionProp.Value;
            Plane plane = new(pos.Position, Vector3.Up);
            return plane;
        }
        // Builds the visual quad representing the gizmo and registers selection tags.
        public void CreateMesh(IRenderBin renderBin) {
            var refpos = roadNode.PositionProp.Value;
            var azimuth = refpos.Azimuth;
            var radians = GeometryUtils.FieldToRadians(azimuth);
            var lastLane = roadNode.LastLane;
            if (lastLane == null) return;
            var sideOffset = lastLane.RightPosition * 2;
            var rotMatrix = Matrix.CreateRotationY(radians);
            var offsetMatrix = Matrix.CreateTranslation(refpos.Position + Vector3.Transform(new Vector3(sideOffset, 0, 0), rotMatrix));
            var frameMatrix = rotMatrix * offsetMatrix;
            Vector3 p1 = Vector3.Transform(new Vector3(-1, 0,  1), frameMatrix);
            Vector3 p2 = Vector3.Transform(new Vector3(1, 0, 1), frameMatrix);
            Vector3 p3 = Vector3.Transform(new Vector3(1, 0, -1), frameMatrix);
            Vector3 p4 = Vector3.Transform(new Vector3(-1, 0, -1), frameMatrix);
            renderBin.DrawQuad(p1, p2, p3, p4, Color.Red);
            renderBin.AddTagsToLastTriangles(2, this);
        }
    }
}
