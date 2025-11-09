using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TranSimCS.Collections;
using TranSimCS.Geometry;
using TranSimCS.Worlds;

namespace TranSimCS.Tools.Inspect {
    public static class InspectMethods {
        public static ObservableList<Inspector> inspectors = [];

        private static bool Inited = false;
        public static void Init() {
            if (Inited) return;
            Inited = true;
            inspectors.Add(InspectClass);
            inspectors.Add(InspectGUID);
            inspectors.Add(InspectCoords);
        }

        public static string? InspectCoords(object? obj, InspectTool tool) {
            if (obj is IPosition ipos) {
                var posrot = ipos.PositionData;
                var pos = posrot.Position;
                var yaw = GeometryUtils.FieldToDegs(posrot.Azimuth);
                var pitch = MathHelper.ToDegrees(posrot.Inclination);
                var roll = MathHelper.ToDegrees(posrot.Tilt);
                return $"Position: {pos}\nazimuth: {yaw}, pitch: {pitch}, roll: {roll}";
            }
            return null;
        }
        public static string? InspectClass(object? obj, InspectTool tool) {
            if (obj == null) return "No object selected";
            return "CLR class: " + obj.GetType().Name;
        }
        public static string? InspectGUID(object? obj, InspectTool tool) {
            tool.Guid = null;
            if(obj is IGuid entity) {
                tool.Guid = entity.Guid;
                return "GUID: " + entity.Guid;
            }
            return null;
        }
    }
}
