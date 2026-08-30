using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using Silk.NET.Input;

namespace TranSimCS.SilkNet.Mode {
    public class ModeDemolish: IMode {
        public SilkNetTest Window { get; private set; }
        public ModeDemolish(SilkNetTest window) { Window = window; }

        public string Title() => "Bulldozer";
        void IMode.DrawUI() {
            Vector4 red = new(1, 0, 0, 1);
            Vector4 green = new(0, 1, 0, 1);
            Vector4 maroon = new(0.5f, 0, 0, 1);

            if (ImGui.Begin("Bulldozer")) {
                ImGui.Text("[LMB] Demolish the object (red and orange)");
                ImGui.Text("[RMB] Dmomlish the subcomponent (orange only)");
                var selectedObj = Window.MouseOver?.SelectedObj;
                var selectedComponent = Window.MouseOver?.Tag;
                if(selectedObj != null) ImGui.Text($"Currently hovered object: {selectedObj.Guid} {selectedObj}");
                if(selectedComponent != null) ImGui.Text($"Currently hovered component: {selectedComponent}");
                (Vector4, string) objInfo = selectedObj is IDemolish
                    ? (green, "The selected object can be demolished")
                    : (selectedObj == null)
                    ? (maroon, "No object selected")
                    : (red, "The selected object can't be demolished");
                (Vector4, string) componentInfo = selectedComponent is IDemolish
                    ? (green, "The selected component can be demolished")
                    : (selectedComponent == null)
                    ? (maroon, "No component selected")
                    : (red, "The selected component can't be demolished");
                ImGui.TextColored(objInfo.Item1, objInfo.Item2);
                ImGui.TextColored(componentInfo.Item1, componentInfo.Item2);
                ImGui.End();
            }
        }
        void IMode.OnMousePress(MouseButton button) {
            IDemolish? demolishable = null;
            if(button == MouseButton.Left) 
                //Demolish as a whole
                demolishable = Window.MouseOver?.SelectedObj as IDemolish;
            if(button == MouseButton.Right) 
                //Demolish only the selected component
                demolishable = Window.MouseOver?.Tag as IDemolish;
            if(demolishable != null) {
                demolishable.Demolish();
                Window.MouseOver = null;
            }
        }
        HighlightColors IMode.SelectionColors() => HighlightColors.DemolitionHighlightColor;
    }

    public interface IDemolish {
        /// <summary>
        /// Deletes the object from the world
        /// </summary>
        public void Demolish();
    }
}
