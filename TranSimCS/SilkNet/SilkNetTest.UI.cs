using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using TranSimCS.Cars;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;
using TranSimCS.Select;
using TranSimCS.Setting;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet {
    //UI methods for SilkNetTest
    public partial class SilkNetTest {
        private void DrawUI() {
            ImGui.BeginMainMenuBar();
            if (ImGui.BeginMenu("File")) {
                if (ImGui.MenuItem("Load")) {
                    CurrentlyOpenModal = LoadModal;
                    Reload();
                }
                if (ImGui.MenuItem("Save")) {
                    CurrentlyOpenModal = SaveModal;
                    Reload();
                }
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

            //Show the toolbar
            var viewport = ImGui.GetMainViewport();

            float height = ImGui.GetFrameHeightWithSpacing();

            ImGui.SetNextWindowPos(
                new Vector2(
                    viewport.WorkPos.X,
                    viewport.WorkPos.Y + viewport.WorkSize.Y - height));

            ImGui.SetNextWindowSize(
                new Vector2(viewport.WorkSize.X, height));

            var menuBarFlags =
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.MenuBar;

            if (ImGui.Begin("##BottomMenuBar", menuBarFlags)) {
                if (ImGui.BeginMenuBar()) {
                    foreach (var mode in AvailableModes)
                        if (ImGui.MenuItem(mode.Title(), "", mode == Mode)) Mode = mode;
                    ImGui.EndMenuBar();
                }
                ImGui.End();
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

            //Show a compass
            DrawCompass(camera.Azimuth);

            ImGui.EndMainMenuBar();

            CurrentlyOpenModal?.Invoke();

            Mode.DrawUI();

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

        public bool ShowCompass;

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
                    DearUI.InputObjPos("Position/Rotation", positionable);
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
                        Car.LaunchCar(World, strip, SpawnCarVelocity);
                    }
                }
                if(tag is HalfLane lane) {
                    float lpos = lane.Bounds.Min;
                    float cpos = lane.MiddlePosition;
                    float rpos = lane.Bounds.Max;
                    float width = lane.Width;

                    bool dimensionsChanged = false;
                    if(ImGui.DragFloat("Move: L", ref lpos, 0.005f, rpos - 10, rpos)){
                        if (lpos > rpos) lpos = rpos;
                        cpos = (lpos + rpos) / 2;
                        width = rpos - lpos;
                        dimensionsChanged = true;
                    }
                    if(ImGui.DragFloat("C", ref cpos, 0.01f, -100, 100)) {
                        float hwidth = width / 2l;
                        lpos = cpos - hwidth;
                        rpos = cpos + hwidth;
                        dimensionsChanged = true;
                    }
                    if(ImGui.DragFloat("R", ref rpos, 0.005f, lpos, lpos + 10)){
                        if (lpos > rpos) rpos = lpos;
                        cpos = (lpos + rpos) / 2;
                        width = rpos - lpos;
                        dimensionsChanged = true;
                    }
                    if (dimensionsChanged) {
                        lane.Bounds = new(lpos, rpos);
                    }
                }
                if(obj is Car car) {
                    ImGui.DragFloat("Speed [m/s]", ref car.Speed, 0.05f, 0, 100, "%.2f");
                }

                ImGui.End();
            }
        }

        private void LoadModal() {
            if (DearUI.Modal("Load a world")) {
                if (ImGui.Button("Close")) CurrentlyOpenModal = null;
                foreach (var world in Worlds) {
                    if (ImGui.Button(world)) {
                        SaveTitle = world;
                        var worldPath = Path.Combine(Program.SaveRoot, world);
                        log.Info($"Loading a world from path {worldPath}");
                        try {
                            World = TSWorld.LoadFromFile(worldPath);
                            CurrentlyOpenModal = null;
                        } catch (Exception e) {
                            Message error = Message.ErrorMessage("Failed to load the world " + SaveTitle, e, this);
                            CurrentlyOpenModal = error.ShowMessage;
                            log.Error(e);
                            #if (DEBUG)
                            throw;
                            #endif
                        }
                    }
                }
                DearUI.EndModal();
            }
            
        }
        private void SaveModal() {
            if (DearUI.Modal("Save a world")) {
                ImGui.SameLine();
                ImGui.InputText("File name", ref SaveTitle, 99);
                ImGui.SameLine();
                if (ImGui.Button("Save")) {
                    var worldPath = Path.Combine(Program.SaveRoot, SaveTitle);
                    bool fileExists = File.Exists(worldPath);
                    if (fileExists) {
                        //Warn the user that a world will be overwritten
                        CurrentlyOpenModal = DuplicateSaveModal;
                    } else {
                        //Save the world
                        SaveTheWorld();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Close")) CurrentlyOpenModal = null;
                foreach (var world in Worlds) ImGui.TextColored(Colors.Cyan.ToVector4(), world);
                DearUI.EndModal();
            }
        }

        private void DuplicateSaveModal() {
            
            if (DearUI.Modal($"You're about to overwrite a world {SaveTitle}")) {
                ImGui.Text("Do you want to proceed?");
                if (ImGui.Button("Yes")) {
                    //Overwrite the world
                    SaveTheWorld();
                }
                if (ImGui.Button("No")) {
                    //Return to the save modal
                    CurrentlyOpenModal = SaveModal;
                }
                DearUI.EndModal();
            }
            
        }

        private void SaveTheWorld() {
            var worldPath = Path.Combine(Program.SaveRoot, SaveTitle);
            try {
                World.SaveToFile(worldPath);
                CurrentlyOpenModal = null;
            } catch(Exception e) {
                Message error = Message.ErrorMessage("Failed to save the world " + SaveTitle, e, this);
                CurrentlyOpenModal = error.ShowMessage;
                log.Error(e);
                #if (DEBUG)
                throw;
                #endif
            }
        }

        public void CloseModals() => CurrentlyOpenModal = null;
    }
}
