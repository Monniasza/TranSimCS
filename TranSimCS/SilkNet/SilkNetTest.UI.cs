using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Roads.Strip;
using TranSimCS.Setting;
using TranSimCS.Tools;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Cars;

namespace TranSimCS.SilkNet {
    //UI methods for SilkNetTest
    public partial class SilkNetTest {
        private void DrawUI() {
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File")) {
                if (ImGui.MenuItem("Load", "", IsLoadOpen, true)) IsLoadOpen ^= true;
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit")) {
                
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools")) {
                DearUI.MenuToggle("Stats", ref IsStatsOpen);
                DearUI.MenuToggle("Enable examples", ref AreExamplesOpen);
                if(ImGui.MenuItem("Stop tracking")) {
                    TrackPosition = null;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings")) {
                DearUI.InputFloat("Car spawn rate", Settings.CarSpawnRateProp);
                DearUI.MenuToggle("Enable car spawning", Settings.SpawnCarsProp);
                DearUI.MenuToggle("Day/night cycle", Settings.DayNightCycleProp);
                DearUI.InputFloat("Duration of a day", Settings.DayTimeLengthProp);
                DearUI.MenuToggle("Invert all normals", Settings.InvertAllNormalsProp);
                DearUI.MenuToggle("Select road nodes", ref SelectNodes);
                DearUI.MenuToggle("Select road segments", ref SelectSegments);
                DearUI.MenuToggle("Select road sections", ref SelectSections);
                DearUI.MenuToggle("Select cars", ref SelectCars);
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();

            if (IsLoadOpen) {
                ImGui.Begin("Load a world");
                if (ImGui.Button("Reload"))
                    Reload();
                foreach (var world in Worlds) {
                    if (ImGui.Button(world)) {
                        var worldPath = Path.Combine(Program.SaveRoot, world);
                        World = TSWorld.LoadFromFile(worldPath);
                    }
                }
                ImGui.End();
            }
            if (IsStatsOpen) {
                ImGui.Begin("Stats");
                ImGui.Text(Stats.Format());
                ImGui.End();
            }

            if (AreExamplesOpen) {
                ImGui.ShowDemoWindow();
            }
            if (Sticky != null) ShowObjectWindow(Sticky.Value);
        }


        //Object window-specific properties
        public float SpawnCarVelocity = 20;

        private void ShowObjectWindow(Selection selection) {
            var obj = selection.SelectedObj;
            var tag = selection.Tag;
            if (obj == null) return;
            
            if(ImGui.Begin($"Selected object: {obj.GetType()} {obj.Guid}###selection")) {
                ImGui.Text($"Picked coordinates: {selection.Coordinates.X}  {selection.Coordinates.Y}  {selection.Coordinates.Z}");
                ImGui.Text($"Picked tag: {tag}");
                if(obj is IPosition positionable) {
                    DearUI.InputObjPos("Position/Rotation", positionable.PositionProp);
                    if(ImGui.Button("Track this object")) {
                        TrackPosition = positionable;
                    }
                }
                if (tag is ILaneSpec lanespeccable) {
                    DearUI.InputLaneSpec("Lane spec", lanespeccable);
                    if (ImGui.Button("Copy lane spec")) LaneSpec = lanespeccable.LaneSpec;
                    ImGui.SameLine();
                    if (ImGui.Button("Paste lane spec")) lanespeccable.LaneSpec = LaneSpec;
                }
                if(obj is IRoadFinish roadFinishable) {
                    DearUI.InputRoadFinish("Road finish", roadFinishable.FinishProperty);
                    if (ImGui.Button("Copy road finish")) RoadFinish = roadFinishable.FinishProperty.Value;
                    ImGui.SameLine();
                    if (ImGui.Button("Paste road finish")) roadFinishable.FinishProperty.Value = RoadFinish;
                }
                if(tag is LaneStrip strip) {
                    ImGui.DragFloat("Spawn car speed [m/s]", ref SpawnCarVelocity, 0.05f, 0, 100, "%.2f");
                    if(ImGui.Button("Spawn a car")) {
                        CarLauncherTool.LaunchCar(World, strip, SpawnCarVelocity);
                    }
                }
                if(obj is Car car) {
                    ImGui.DragFloat("Speed [m/s]", ref car.Speed, 0.05f, 0, 100, "%.2f");
                }

                ImGui.End();
            }
        }
    }
}
