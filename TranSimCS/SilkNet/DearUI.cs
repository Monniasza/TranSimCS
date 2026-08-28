using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Geometry;
using TranSimCS.Property;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Dear ImGui extensions for TranSim types, like <see cref="Property{T}"/>
    /// </summary>
    public static class DearUI {
        public static void MenuToggle(string name, ref bool attribute, string shortcut = "", bool enabled = true) {
            attribute ^= ImGui.MenuItem(name, shortcut, attribute, enabled);
        }
        public static void MenuToggle(string name, Property<bool> attribute, string shortcut = "", bool enabled = true) {
            var tmp = attribute.Value;
            MenuToggle(name, ref tmp, shortcut, enabled);
            attribute.Value = tmp;
        }

        public static bool TextField(string title, Property<string> text, uint maxLength = uint.MaxValue) {
            var tmp = text.Value;
            bool changed = ImGui.InputText(title, ref tmp, maxLength);
            if (changed) text.Value = tmp;
            return changed;
        }
        public static bool InputFloat(string title, Property<float> vector) {
            var tmp = vector.Value;
            bool changed = ImGui.InputFloat(title, ref tmp);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat2(string title, Property<Vector2> vector) {
            var tmp = vector.Value;
            bool changed = ImGui.InputFloat2(title, ref tmp);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat3(string title, Property<Vector3> vector) {
            var tmp = vector.Value;
            bool changed = ImGui.InputFloat3(title, ref tmp);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat4(string title, Property<Vector4> vector) {
            var tmp = vector.Value;
            bool changed = ImGui.InputFloat4(title, ref tmp);
            if (changed) vector.Value = tmp;
            return changed;
        }

        public static bool InputObjPos(string title, ref PositionEulerAngles pea) {
            ImGui.BeginChild(title);
            bool posChanged = ImGui.InputFloat3("Position [m]", ref pea.Position);
            var yawPitchRoll = pea.YawPitchRoll.ToDegrees();
            bool rotChanged = ImGui.InputFloat3("Yaw/pitch/roll [degs]", ref yawPitchRoll);
            if(rotChanged) pea.YawPitchRoll = yawPitchRoll.ToRadians();
            ImGui.EndChild();
            return posChanged | rotChanged;
        }
        public static bool InputObjPos(string title, Property<PositionEulerAngles> pea) {
            var tmp = pea.Value;
            var result = InputObjPos(title, ref tmp);
            if(result) pea.Value = tmp;
            return result;
        }
    }
}
