using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;
using TranSimCS.Model;
using TranSimCS.Tools.Panels;

namespace TranSimCS.Tools {
    public interface ITool {
        //Essential attribute methods
        public string Name { get; }
        public string Description { get; }
        public (object[], string)[] PromptKeys();
        public void AddAttributes(ISet<string> action) { }

        //Event handling methods
        public void OnClick(MouseButton button) { }
        public void OnRelease(MouseButton button) { }
        public void OnKeyDown(Keys key) { }
        public void OnKeyUp(Keys key) { }
        public void OnOpen() { }
        public void OnClose() { }

        //Update/draw methods
        public void Update(GameTime gameTime) { }
        public void Draw(GameTime gameTime) { }
        public void Draw2D(GameTime gameTime) { }
        public void AddSelectors(MultiMesh invisibleSelectors, MultiMesh visibleSelectors) { }

        public static void Init() {
            ToolsPanel.AddPanel(ToolAttribs.showRoadTools, (x => new RoadTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showFinishes, (x => new FinishTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showPosManip, (x => new PrecPosTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showChooser, (x => new PickAnObjectTab(x)));
            ToolsPanel.AddPanel(ToolAttribs.showSnapOptions, (x => new SnappingPanel(x)));
            ToolsPanel.AddPanel(ToolAttribs.showLaneSpecs, (x => new LaneSpecTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showLaneManip, (x => new LaneTools(x)));
            ToolsPanel.AddPanel(ToolAttribs.showSettings, (x => new GlobalSettingsTab(x)));
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
        public const string showLaneManip = "menuLane";
        public const string showStats = "menuStats";
        public const string showSettings = "menuSettings";
    }
}
