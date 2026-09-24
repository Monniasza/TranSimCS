using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using TranSimCS.Cars;
using TranSimCS.Mode;
using TranSimCS.Mode.NodeEditor;
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
                    SaveGuard(LoadDialog);
                }
                if (ImGui.MenuItem("Save")) {
                    CurrentlyOpenModal = SaveModal;
                    Reload();
                }
                if (ImGui.MenuItem("New world")) {
                    SaveGuard(() => World = new TSWorld());
                }
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
                DearUI.InputFloat("Duration of a day", Settings.DayTimeLengthProp, 1, 0);
                DearUI.MenuToggle("Invert all normals", Settings.InvertAllNormalsProp);
                DearUI.MenuToggle("Select road nodes", ref SelectNodes);
                DearUI.MenuToggle("Select road segments", ref SelectSegments);
                DearUI.MenuToggle("Select road sections", ref SelectSections);
                DearUI.MenuToggle("Select cars", ref SelectCars);
                ImGui.DragFloat("Simulation speed", ref SimulationSpeed, 0.001f, 0, 32);
                ImGui.EndMenu();
            }

            //Show a compass
            DrawCompass(camera.Azimuth);

            ImGui.EndMainMenuBar();

            CurrentlyOpenModal?.Invoke();

            Mode.DrawUI();

            DrawCursor();

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

        private void DrawCursor() {
            if (IsMouseOverUI || CurrentlyOpenModal != null) return;

            var cursor = Mode.GetCursor();
            if (cursor == CursorType.Default) return;

            ImGui.SetMouseCursor(ImGuiMouseCursor.None);

            var mousePos = ImGui.GetIO().MousePos;
            var drawList = ImGui.GetForegroundDrawList();
            const float fontSize = 24f;

            string symbol;
            Vector4 color;
            switch (cursor) {
                case CursorType.Add:
                    symbol = "+";
                    color = new Vector4(0, 1, 0, 1);
                    break;
                case CursorType.Remove:
                    symbol = "-";
                    color = new Vector4(1, 0.27f, 0, 1);
                    break;
                case CursorType.Unavailable:
                    symbol = "X";
                    color = new Vector4(0.5f, 0, 0, 1);
                    break;
                case CursorType.Open:
                    symbol = "O";
                    color = new Vector4(1, 1, 1, 1);
                    break;
                default:
                    return;
            }

            var textSize = ImGui.CalcTextSize(symbol);

            drawList.AddCircleFilled(mousePos, 12f, ImGui.GetColorU32(new Vector4(0, 0, 0, 0.5f)), 16);
            var textPos = mousePos - textSize;
            drawList.AddText(ImGui.GetFont(), fontSize, textPos, ImGui.GetColorU32(color), symbol);
        }

        /// <summary>
        /// Shows an error to the user in a modal message box. Use this to report a problem that the user
        /// can act on, such as an invalid lane mapping, rather than letting the exception escape.
        /// </summary>
        /// <param name="title">Short heading, e.g. "Invalid lane mapping".</param>
        /// <param name="text">The problem, in terms the user can act on.</param>
        /// <param name="details">Optional technical detail, shown collapsed.</param>
        public void ShowError(string title, string text, string? details = null) {
            var message = details is null
                ? new Message(title, text, [new MessageAction("OK", () => CurrentlyOpenModal = null)])
                : new Message(title, text, details, this);
            CurrentlyOpenModal = message.ShowMessage;
        }

        /// <summary>
        /// Shows an error to the user in a modal message box, taking the text from the exception.
        /// </summary>
        public void ShowError(string title, Exception exception)
            => ShowError(title, exception.Message, exception.ToString());

        private void SaveGuard(Action accepted) {
            var message = new Message("Unsaved changes", "Do you want to continue? Any unsaved changes will be lost.", [
                new MessageAction("OK", accepted),
                new MessageAction("Cancel", () => CurrentlyOpenModal = null)
            ]);
            CurrentlyOpenModal = message.ShowMessage;
        }
        private void LoadDialog() {
            CurrentlyOpenModal = LoadModal;
            Reload();
        }

        private const string SaveFileExtension = ".transim";

        private void ShowSaveError(string text) {
            Message error = new("Invalid file name", text, [new MessageAction("OK", () => CurrentlyOpenModal = SaveModal)]);
            CurrentlyOpenModal = error.ShowMessage;
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



                ImGui.BeginTabBar("object-tabs");
                if (obj is IPosition positionable && ImGui.BeginTabItem("Position")) {
                    DearUI.InputObjPos("Position/Rotation", positionable);
                    if(ImGui.Button("Track this object")) {
                        TrackPosition = positionable;
                    }
                    ImGui.EndTabItem();
                }
                if (tag is ILaneSpec lanespeccable && ImGui.BeginTabItem("Lane spec")) {
                    DearUI.InputLaneSpec("Lane spec", lanespeccable);
                    if (ImGui.Button("Copy lane spec")) LaneSpec = lanespeccable.LaneSpec;
                    ImGui.SameLine();
                    if (ImGui.Button("Paste lane spec")) lanespeccable.LaneSpec = LaneSpec;
                    ImGui.EndTabItem();
                }
                if(obj is IRoadFinish roadFinishable && ImGui.BeginTabItem("Road finish")) {
                    DearUI.InputRoadFinish("Road finish", roadFinishable.FinishProperty);
                    if (ImGui.Button("Copy road finish")) RoadFinish = roadFinishable.FinishProperty.Value;
                    ImGui.SameLine();
                    if (ImGui.Button("Paste road finish")) roadFinishable.FinishProperty.Value = RoadFinish;
                    ImGui.EndTabItem();
                }
                if(tag is LaneStrip strip && ImGui.BeginTabItem("Lane strip")) {
                    ImGui.DragFloat("Spawn car speed [m/s]", ref SpawnCarVelocity, 0.05f, 0, 100, "%.2f");
                    if(ImGui.Button("Spawn a car")) {
                        Car.LaunchCar(World, strip, SpawnCarVelocity);
                    }
                    ImGui.EndTabItem();
                }
                if(tag is HalfLane lane && ImGui.BeginTabItem("Lane editor")) {
                    NodeEditorUI.EditHalfLaneBorders(lane);
                    ImGui.EndTabItem();
                }
                if(obj is Car car && ImGui.BeginTabItem("Car")) {
                    ImGui.DragFloat("Speed [m/s]", ref car.Speed, 0.05f, 0, 100, "%.2f");
                    ImGui.EndTabItem();
                }
                if(obj is RoadNode node && ImGui.BeginTabItem("Road node editor")) {
                    NodeEditorUI.ShowNodeEditor(node.FrontHalf, ref RoadNodeEditorHalfLane, ref LaneSpec);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
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
                bool confirmed = ImGui.InputText("File name", ref SaveTitle, 99, ImGuiInputTextFlags.EnterReturnsTrue);
                ImGui.SameLine();
                if (confirmed | ImGui.Button("Save")) {
                    SaveTitle = SaveTitle.Trim();
                    if (string.IsNullOrWhiteSpace(SaveTitle)) {
                        ShowSaveError("The file name cannot be empty.");
                    } else if (SaveTitle.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                        ShowSaveError("The file name contains invalid characters (like \\ / : * ? \" < > |).");
                    } else {
                        if(!SaveTitle.EndsWith(SaveFileExtension, StringComparison.OrdinalIgnoreCase))
                            SaveTitle += SaveFileExtension;
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
