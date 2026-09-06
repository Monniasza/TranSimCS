using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using TranSimCS.Mode.RoadConstruction;
using TranSimCS.Roads;
using TranSimCS.SilkNet.RoadConstruction;
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
                ImGui.DragInt("##leftLanes", ref LeftLanes, 0, 20);
                ImGui.TableNextColumn();
                ImGui.DragFloat("##medianWidth", ref MedianWidth, 0, 100);
                ImGui.TableNextColumn();
                ImGui.DragInt("##rightLanes", ref RightLanes, 0, 20);

                ImGui.EndTable();
            }
        }

        //Lane spec
        public LaneSpec LaneSpec = LaneSpec.Default;
        public void ShowLaneCreator() => DearUI.InputLaneSpec("Lane spec", ref LaneSpec);

        //Road finish
        public RoadFinish RoadFinish = RoadFinish.Embankment;
        public void ShowFinishSettings() => DearUI.InputRoadFinish("Road finish", ref RoadFinish);

        //Snapping properties
        public readonly SnappingGrid snappingGrid;
        public bool SnappingEnabled = false;
        
        public void ShowSnappingSettings() {
            ImGui.Text("Snapping grid");
            DearUI.InputInt("Snapping cell count", snappingGrid.CellCountProp, 0, 1, 200);
            DearUI.InputFloat("Snapping cell size", snappingGrid.CellSizeProp, 0.01f, 0, 100);
            DearUI.MenuToggle("Snapping is infinite", snappingGrid.IsInfiniteProp);
            DearUI.MenuToggle("Is the grid horizontal?", snappingGrid.IsHorizontalProp);
            DearUI.MenuToggle("Enable snapping", ref SnappingEnabled);
        }

        //Segment presets
        public static readonly ImmutableArray<RoadMode> RoadModes = [
            new StraightMode(), new CircMode(), new SBendMode(), new FromReferenceMode()
        ];
        public RoadPresets SegmentPresets;
        public void ShowSegmentPresets() {
            ImGui.Text("Segment presets");
            ImGui.Checkbox("Flatten inclination", ref SegmentPresets.IsInclineFlat);
            ImGui.Checkbox("Flatten tilt", ref SegmentPresets.IsTiltFlat);
            string[] roadModeNames = ["Straight", "Circular arc", "Serpentine", "From snapping grid"];
            for (int i = 0; i < 4; i++) {
                if (i != 0) ImGui.SameLine();
                bool isActive = RoadModes[i] == SegmentPresets.RoadMode;
                if (ImGui.Checkbox(roadModeNames[i], ref isActive)) SegmentPresets.RoadMode = RoadModes[i];
            }
            Alignment[] alignments = Enum.GetValues<Alignment>();
            string[] alignmentNames = Enum.GetNames<Alignment>();
            for(int i = 0; i < alignments.Length; i++) {
                if (i != 0) ImGui.SameLine();
                bool isActive = alignments[i] == SegmentPresets.Alignment;
                if(ImGui.Checkbox(alignmentNames[i], ref isActive)) SegmentPresets.Alignment = alignments[i];
            }
            ImGui.DragFloat("Height [m]", ref SegmentPresets.Height, SegmentPresets.HeightStep / 200);
            ImGui.DragFloat("Height step [m]", ref SegmentPresets.HeightStep, 0.005f);

            ImGui.Text("[?] to swap sides");
            ImGui.DragInt("Expand/merge left lanes [Q/E]", ref SegmentPresets.AddRemoveLeft, 0.01f, -100, 100);
            ImGui.DragInt("Expand/merge right lanes [O/P]", ref SegmentPresets.AddRemoveRight, 0.01f, -100, 100);
            DearUI.InputUInt("Include/Exclude left lanes [Z/C]", ref SegmentPresets.IncludeExcludeLeft, 0.01f, 0, 100);
            DearUI.InputUInt("Include/Exclude right lanes [,/.]", ref SegmentPresets.IncludeExcludeRight, 0.01f, 0, 100);
            DearUI.MenuToggle("Check for counts to be inclusive. Uncheck for exclusive", ref SegmentPresets.IsInclusive, "X");
            DirectionChoice[] options = Enum.GetValues<DirectionChoice>();
            string[] names = Enum.GetNames<DirectionChoice>();
            for (int i = 0; i < 3; i++) {
                if (i != 0) ImGui.SameLine();
                bool isActive = SegmentPresets.DirectionChoice == options[i];
                if(ImGui.Checkbox("Direction: " + names[i], ref isActive)) SegmentPresets.DirectionChoice = options[i];
            }

            
        }
    }
}
