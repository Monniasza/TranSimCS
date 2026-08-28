using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Worlds;

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
        }
    }
}
