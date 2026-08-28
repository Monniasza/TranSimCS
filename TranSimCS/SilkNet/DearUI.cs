using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Silk.NET.Vulkan;
using TranSimCS.Geometry;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Tools;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Dear ImGui extensions for TranSim types, like <see cref="Property{T}"/>
    /// </summary>
    public static class DearUI {
        public static bool MenuToggle(string name, ref bool attribute, string shortcut = "", bool enabled = true) {
            bool changed = ImGui.MenuItem(name, shortcut, attribute, enabled);
            attribute ^= changed;
            return changed;
        }
        public static bool MenuToggle(string name, Property<bool> attribute, string shortcut = "", bool enabled = true) {
            var tmp = attribute.Value;
            var changed = MenuToggle(name, ref tmp, shortcut, enabled);
            attribute.Value = tmp;
            return changed;
        }

        public static bool TextField(string title, Property<string> text, uint maxLength = uint.MaxValue) {
            var tmp = text.Value;
            bool changed = ImGui.InputText(title, ref tmp, maxLength);
            if (changed) text.Value = tmp;
            return changed;
        }
        public static bool InputFloat(string title, Property<float> vector, float velocity = 1, float min = float.NegativeInfinity, float max = float.PositiveInfinity, string format = "%.6f") {
            var tmp = vector.Value;
            bool changed = ImGui.DragFloat(title, ref tmp, velocity, min, max, format);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat2(string title, Property<Vector2> vector, float velocity = 1, float min = float.NegativeInfinity, float max = float.PositiveInfinity, string format = "%.6f") {
            var tmp = vector.Value;
            bool changed = ImGui.DragFloat2(title, ref tmp, velocity, min, max, format);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat3(string title, Property<Vector3> vector, float velocity = 1, float min = float.NegativeInfinity, float max = float.PositiveInfinity, string format = "%.6f") {
            var tmp = vector.Value;
            bool changed = ImGui.DragFloat3(title, ref tmp, velocity, min, max, format);
            if (changed) vector.Value = tmp;
            return changed;
        }
        public static bool InputFloat4(string title, Property<Vector4> vector, float velocity = 1, float min = float.NegativeInfinity, float max = float.PositiveInfinity, string format = "%.6f") {
            var tmp = vector.Value;
            bool changed = ImGui.DragFloat4(title, ref tmp, velocity, min, max, format);
            if (changed) vector.Value = tmp;
            return changed;
        }

        public static bool InputObjPos(string title, ref PositionEulerAngles pea) {
            ImGui.Text(title);
            bool posChanged = ImGui.DragFloat3("Position [m]", ref pea.Position, 1, -100000, 100000, "%.3f");
            var yawPitchRoll = pea.YawPitchRoll.ToDegrees();
            bool rotChanged = ImGui.DragFloat3("Yaw/pitch/roll [degs]", ref yawPitchRoll, 0.5f, -360, 360, "%.1f");
            if(rotChanged) pea.YawPitchRoll = yawPitchRoll.ToRadians();
            return posChanged | rotChanged;
        }
        public static bool InputObjPos(string title, Property<PositionEulerAngles> pea) {
            var tmp = pea.Value;
            var result = InputObjPos(title, ref tmp);
            if(result) pea.Value = tmp;
            return result;
        }
        public static bool InputLaneSpec(string title, ref LaneSpec laneSpec) {
            ImGui.Text(title);

            var colorVector = laneSpec.Color.ToVector4();
            var colorChanged = ImGui.ColorEdit4("Color", ref colorVector);
            if(colorChanged) laneSpec.Color = new Color(colorVector);

            var widthChanged = ImGui.DragFloat("Width [m]", ref laneSpec.Width, 0.01f, 0, 100, "%.2f");
            var speedChanged = ImGui.DragFloat("Speed limit [km/h]", ref laneSpec.SpeedLimit, 0.1f, 0, 1000, "%.1f");
            var lineChanged = ImGui.DragFloat("Line width [m]", ref laneSpec.LineWidth, 0.001f, 0, 10, "%.3f");

            var result = colorChanged | widthChanged | speedChanged | lineChanged;

            //Vehicle types
            LaneSpecToolsFlag<VehicleTypes>[] vehicleTypes = [
                new("Car", "ui/car", null, VehicleTypes.Car),
                new("Truck", "ui/truck", null, VehicleTypes.Truck),
                new("Bus", "ui/bus", null, VehicleTypes.Bus),
                new("Bike", "ui/check", null, VehicleTypes.Bicycle),
                new("Pedestrian", "ui/check", null, VehicleTypes.Pedestrian),
                new("Light rail", "ui/train", null, VehicleTypes.LRT),
                new("Train", "ui/train", null, VehicleTypes.Train),
                new("Horse", "ui/check", null, VehicleTypes.Horse),
                new("Airplanes", "ui/check", null, VehicleTypes.Plane),
                new("Rockets", "ui/check", null, VehicleTypes.Rocket),
            ];
            LaneSpecToolsFlag<LaneFlags>[] flags = [
                new("Allow reversing & wrong way", "signs/bidirectional", null, LaneFlags.AllowReverse),
                new("Stop", "signs/stop", null, LaneFlags.Stop),
                new("Yield", "signs/yield", null, LaneFlags.Yield),
                new("Parking", "signs/parking", null, LaneFlags.Parking),
                new("Sidewalk", "ui/crosswalk", null, LaneFlags.Sidewalk),
                new("Platform", "ui/bus", null, LaneFlags.Platform),
                new("Merge or expand right", "signs/mergeright", null, LaneFlags.MergeRight),
                new("Merge or expand left", "signs/mergeleft", null, LaneFlags.MergeLeft),
                new("No switching to the left", "signs/noleft", null, LaneFlags.NoLeft),
                new("No switching to the right", "signs/noright", null, LaneFlags.NoRight),
                new("Merge/Expand", "signs/merge", "signs/expand", LaneFlags.IsMerge),
            ];
            (string, LaneSpec)[] specPresets = [
                ("Default on road", LaneSpec.Default),
                ("Bike lane", LaneSpec.Bicycle),
                ("Sidewalk", LaneSpec.Pedestrian),
                ("Path", LaneSpec.Path),
                ("Motorway", LaneSpec.Motorway),
                ("Bus lane", LaneSpec.Bus),
                ("Platform", LaneSpec.Platform),
                ("Empty", LaneSpec.None)
            ];
            (string, VehicleTypes)[] vehiclePresets = [
                ("Clear all", VehicleTypes.None),
                ("Motor vehicles", VehicleTypes.MotorVehicles),
                ("Non-motorized traffic", VehicleTypes.Path),
                ("Vehicles", VehicleTypes.Vehicles),
                ("Aircraft", VehicleTypes.Aircraft),
                ("All road vehicles", VehicleTypes.Vehicles),
                ("All road traffic", VehicleTypes.Transport),
                ("Railway", VehicleTypes.Rail),
                ("Everything", VehicleTypes.All)
            ];

            foreach (var vehicleType in vehicleTypes) {
                bool enabled = laneSpec.VehicleTypes.HasFlags(vehicleType.Flag);
                bool changed = MenuToggle($"Vehicle type flag: {vehicleType.Title}", ref enabled);
                if (changed) {
                    result = true;
                    laneSpec.VehicleTypes ^= vehicleType.Flag;
                }
            }
            foreach (var flag in flags) {
                bool enabled = laneSpec.Flags.HasFlags(flag.Flag);
                bool changed = MenuToggle($"Lane flag: {flag.Title}", ref enabled);
                if (changed) {
                    result = true;
                    laneSpec.Flags ^= flag.Flag;
                }
            }
            if (ImGui.BeginMenu("Pick vehicle/lane spec presets")) {
                foreach(var specPreset in specPresets) {
                    bool clicked = ImGui.MenuItem($"Lane spec preset: {specPreset.Item1}");
                    var changed = clicked && specPreset.Item2 != laneSpec;
                    if (changed) {
                        result = true;
                        laneSpec = specPreset.Item2;
                    }
                }
                foreach (var vehiclePreset in vehiclePresets) {
                    bool clicked = ImGui.MenuItem($"Vehicle preset: {vehiclePreset.Item1}");
                    var changed = clicked && vehiclePreset.Item2 != laneSpec.VehicleTypes;
                    if (changed) {
                        result = true;
                        laneSpec.VehicleTypes = vehiclePreset.Item2;
                    }
                }
                ImGui.EndMenu();
            }
            return result;
        }

        public static bool InputLaneSpec(string title, ILaneSpec editable) {
            var laneSpec = editable.LaneSpec;
            var changed = InputLaneSpec(title, ref laneSpec);
            if (changed) editable.LaneSpec = laneSpec;
            return changed;
        }

        public static bool InputRoadFinish(string title, ref RoadFinish roadFinish) {
            bool changed = ImGui.DragFloat("Height [m]", ref roadFinish.depth, 0.05f, 0, 20, "%.3f");
            float degrees = float.RadiansToDegrees(roadFinish.angle);
            ImGui.Text(title);
            if(ImGui.DragFloat("Angle [degs]", ref degrees)) {
                roadFinish.angle = float.DegreesToRadians(degrees);
                changed = true;
            }
            var surfaces = Enum.GetValues<Surface>();
            if (ImGui.BeginMenu("Surface")) {
                foreach (var surface in surfaces) {
                    var name = Enum.GetName(surface);
                    bool srfChanged = ImGui.MenuItem(name, "", roadFinish.subsurface == surface, true);
                    if(srfChanged) {
                        changed = true;
                        roadFinish.subsurface = surface;
                    }
                }
                ImGui.EndMenu();
            }
            return changed;
        }
        public static bool InputRoadFinish(string title, IProperty<RoadFinish> property) {
            var finish = property.Value;
            var changed = InputRoadFinish(title, ref finish);
            if (changed) {
                property.Value = finish;
            }
            return changed;
        }
    }
}
