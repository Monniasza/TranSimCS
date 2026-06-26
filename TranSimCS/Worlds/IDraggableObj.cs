using System.Collections.Generic;
using System.Linq;
using LanguageExt.ClassInstances;
using Microsoft.Xna.Framework;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Worlds {
    //Component-interfaces for objects
    public interface IDraggableObj {
        public IPosition[] DraggableComponents();
        public Plane DragPlane() => InGameMenu.groundPlane;
    }
    public static class DragMethods {
        public static void Drag(this IEnumerable<IDraggableObj> obj, Vector3 delta) => Drag(obj.SelectMany(x => x.DraggableComponents()), delta);
        public static void Drag(this IDraggableObj obj, Vector3 delta) => Drag(obj.DraggableComponents(), delta);
        public static void Drag(this IEnumerable<IPosition> objects, Vector3 delta) {
            var dedup = objects.ToHashSet();
            foreach (var p in dedup) {
                var objpos = p.PositionData;
                objpos.Position += delta;
                p.PositionData = objpos;
            }
        }

        public static void RotateOld(this IDraggableObj obj, int fieldAzimuth, float pitch, float tilt) {
            foreach (var component in obj.DraggableComponents()) {
                var objpos = component.PositionData;
                objpos.Azimuth += fieldAzimuth;
                objpos.Inclination += pitch;
                objpos.Tilt += tilt;
                component.PositionData = objpos;
            }
        }
        public static void Transform(this IEnumerable<IPosition> objs, TransformQ transform, Vector3 pivot) {
            foreach (var obj in objs.Distinct()) Transform(obj, transform, pivot);
        }
        public static void Transform(this IPosition obj, TransformQ transform, Vector3 pivot) {
            var objPos = obj.PositionData;
            var quatPos = objPos.ToTransformQ();
            quatPos = quatPos.Append(transform, pivot);
            objPos = quatPos.ToObjPos();
            obj.PositionData = objPos;
        }

        public static Vector3 FindCenter(this IDraggableObj obj) => FindCenter(obj.DraggableComponents());
        public static Vector3 FindCenter(this IEnumerable<IPosition> objs) {
            int count = 0;
            Vector3 sum = Vector3.Zero;
            foreach (var obj in objs.Distinct()) {
                sum += obj.PositionData.Position;
                count++;
            }
            return (count == 0) ? Vector3.Zero : (sum / count);
        }
    }
}
