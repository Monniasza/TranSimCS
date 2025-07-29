using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.Gizmo {
    public class TiltGizmo : IDraggableObj {
        public readonly RoadNode roadNode;
        public TiltGizmo(RoadNode roadNode) {
            this.roadNode = roadNode;
        }

        public void Drag(Vector3 vector, Vector3 dragFrom) {
            throw new NotImplementedException();
        }
        Plane IDraggableObj.DragPlane() {
            var frame = roadNode.PositionProp.Value.CalcReferenceFrame();
            var vert = frame.Y;
            var pos = frame.Transform(new Vector3(0, 10, 0));
            Plane plane = new(pos, vert);
            return plane;
        }
    }

    public class AzimuthGizmo(RoadNode roadNode) : IDraggableObj {
        public void Drag(Vector3 vector, Vector3 dragFrom) {
            var newPoint = vector + dragFrom;
            var oldRadius = dragFrom - roadNode.CenterPosition;
            var oldAzimuth = MathF.Atan2(oldRadius.X, oldRadius.Z);
            var newRadius = newPoint - roadNode.CenterPosition;
            var newAzimuth = MathF.Atan2(newRadius.X, newRadius.Z);
            var rotation = newAzimuth - oldAzimuth;
            var fieldRotation = Geometry.RadiansToField(rotation);
            var frame = roadNode.PositionProp.Value;
            frame.Azimuth += fieldRotation;
            roadNode.PositionProp.Value = frame;
        }

        Plane IDraggableObj.DragPlane() {
            var pos = roadNode.PositionProp.Value;
            Plane plane = new(pos.Position, Vector3.Up);
            return plane;
        }
        public void CreateMesh(IRenderBin renderBin) {
            var refpos = roadNode.PositionProp.Value;
            var azimuth = refpos.Azimuth;
            var radians = Geometry.FieldToRadians(azimuth);
            var sideOffset = roadNode.LastLane.RightPosition * 2;
            var rotMatrix = Matrix.CreateRotationY(radians);
            var offsetMatrix = Matrix.CreateTranslation(refpos.Position + Vector3.Transform(new(sideOffset, 0, 0), rotMatrix));
            var frameMatrix = rotMatrix * offsetMatrix;
            Vector3 p1 = Vector3.Transform(new(-1, 0,  1), frameMatrix);
            Vector3 p2 = Vector3.Transform(new( 1, 0,  1), frameMatrix);
            Vector3 p3 = Vector3.Transform(new( 1, 0, -1), frameMatrix);
            Vector3 p4 = Vector3.Transform(new(-1, 0, -1), frameMatrix);
            renderBin.DrawQuad(p1, p2, p3, p4, Color.Red);
            renderBin.AddTagsToLastTriangles(2, this);
        }
    }
}
