using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Roads;
using TranSimCS.Snapping;

namespace TranSimCS.SilkNet {
    public partial class SilkNetTest {
        //Lane properties
        public int LeftLanes;
        public int RightLanes;
        public float MedianWidth;
        public void ShowNodeCreator() {
            if (ImGui.BeginTable("Adjust lane counts and median width", 3)) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Lanes on the left");
                ImGui.TableNextColumn();
                ImGui.Text("Median [m]");
                ImGui.TableNextColumn();
                ImGui.Text("Lanes on the right");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.SliderInt("", ref LeftLanes, 0, 20);
                ImGui.TableNextColumn();
                ImGui.SliderFloat("", ref MedianWidth, 0, 100);
                ImGui.TableNextColumn();
                ImGui.SliderInt("", ref RightLanes, 0, 20);

                ImGui.EndTable();
            }
        }


        public LaneSpec LaneSpec = LaneSpec.Default;
        public void ShowLaneCreator() => DearUI.InputLaneSpec("Lane spec", ref LaneSpec);

        //Snapping properties
        public readonly SnappingGrid snappingGrid;
        public bool SnappingEnabled = false;
        public void ShowSnappingSettings() {
            ImGui.Text("Snapping grid");
            DearUI.InputInt("Snapping cell count", snappingGrid.CellCountProp, 0, 200);
            DearUI.InputFloat("Snapping cell size", snappingGrid.CellSizeProp, 0.01f, 0, 100);
            DearUI.MenuToggle("Snapping is infinite", snappingGrid.IsInfiniteProp);
            DearUI.MenuToggle("Is the grid horizontal?", snappingGrid.IsHorizontalProp);
            DearUI.MenuToggle("Enable snapping", ref SnappingEnabled);
        }
    }
}
