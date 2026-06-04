using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Tools.Panels;
using static MLEM.Ui.Elements.Paragraph;

namespace TranSimCS.Tools {
    public interface ITool {
        public string Name { get; }
        public string Description { get; }
        public void OnClick(MouseButton button) { }
        public void OnRelease(MouseButton button) { }
        public void OnKeyDown(Keys key) { }
        public void OnKeyUp(Keys key) { }
        public void Update(GameTime gameTime);
        public void Draw(GameTime gameTime);
        public void Draw2D(GameTime gameTime);
        public void AddSelectors(MultiMesh invisibleSelectors, MultiMesh visibleSelectors) { }
        public (object[], string)[] PromptKeys();

        public void OnOpen() { }
        public void OnClose() { }

        public void AddAttributes(ISet<string> action) {}

        public static void Init() {
            ToolsPanel.AddPanel(ToolAttribs.showRoadTools, (x => new RoadTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showFinishes, (x => new RoadFinishTab(x)));
            ToolsPanel.AddPanel(ToolAttribs.showDumpTools, (x => new DumpingMenu(x)));
            ToolsPanel.AddPanel(ToolAttribs.showPosManip, (x => new PrecPosTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showChooser, (x => new PickAnObjectTab(x)));
            ToolsPanel.AddPanel(ToolAttribs.showSnapOptions, (x => new SnappingPanel(x)));
            ToolsPanel.AddPanel(ToolAttribs.showLaneSpecs, (x => new RoadConfigurator(x)));
        }
    }
    public static class ToolAttribs {
        public const string noHighlights = "!highlight";
        public const string addLaneSelection = "als";

        //UI attributes
        public const string showFinishes = "menuFinish";
        public const string showRoadTools = "menuRoadTools";
        public const string showLaneSpecs = "menuLaneSpec";
        public const string showMoveTools = "menuMove";
        public const string showDumpTools = "menuDump";
        public const string showPosManip = "menuPos";
        public const string noShift = "disableShift";
        public const string showChooser = "menuChooser";
        public const string disableMMBSnap = "disableSnapKeybind";
        public const string showSnapOptions = "menuSnap";
    }

    public class PaintTool(InGameMenu game) : ITool {
        string ITool.Name => "Paint and pick lane specs";

        string ITool.Description => "Click on roads to set their lane specs";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], " on roads to set their lane specs"),
            ([MouseButton.Right], " near a node to select its lane spec"),
            ([MouseButton.Right], " in the middle of a lane strip for the lane strip's spec")
        ];

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        void ITool.OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                var laneSpec = game.configuration.LaneSpec;
                var selection = game.MouseOverRoad;
                var lane = selection?.SelectedLane;
                if(lane != null) lane.Spec = laneSpec;
                var strip = selection?.SelectedLaneStrip;
                if (strip != null) strip.Spec = laneSpec;
            }
            if (button == MouseButton.Right) {
                var selection = game.MouseOverRoad;
                var laneSpec = selection?.SelectedLaneStrip?.Spec;
                var nodeSpec = selection?.SelectedLane?.Spec;
                var spec = nodeSpec ?? laneSpec;
                if (spec == null) return;
                game.configuration.LaneSpec = spec.Value;
            }
        }

        void ITool.OnKeyDown(Keys key) {
            //unused
        }

        void ITool.OnKeyUp(Keys key) {
            //unused
        }

        void ITool.OnRelease(MouseButton button) {
            //unused
        }

        void ITool.Update(GameTime gameTime) {
            //unused
        }
    }

    public class EditNodeTool : ITool {
        public string Name => "Edit road nodes";

        public string Description => "";

        public (object[], string)[] PromptKeys() => [
            ([MouseButton.Left], "dummy"),
        ];

        public void Draw(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public void Draw2D(GameTime gameTime) {
            throw new NotImplementedException();
        }

        public void OnClick(MouseButton button) {
            throw new NotImplementedException();
        }

        public void OnKeyDown(Keys key) {
            throw new NotImplementedException();
        }

        public void OnKeyUp(Keys key) {
            throw new NotImplementedException();
        }

        public void OnRelease(MouseButton button) {
            throw new NotImplementedException();
        }

        public void Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}
