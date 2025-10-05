using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using NLog;
using TextCopy;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;

namespace TranSimCS.Tools {
    public class InspectTool(InGameMenu game) : ITool {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public string Name => "Inspect";

        public string Description { get; private set; } = "Inspect objects";

        public Guid? Guid { get; private set; }

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public void OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                String? guidString = Guid?.ToString();
                //Copy the GUID to the clipboard
                if(guidString != null) ClipboardService.SetText(guidString);
                var none = "none";
                log.Info($"Selected GUID: {guidString ?? none}");
            }
        }

        public void OnKeyDown(Keys key) {
            //unused
        }

        public void OnKeyUp(Keys key) {
            //unused
        }

        public void OnRelease(MouseButton button) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            return [
                ([], "Hover on objects to discover their parameters"),
                ([MouseButton.Left], "to copy the GUID")
            ];
        }

        public void Update(GameTime gameTime) {
            //TODO: Add expandability for more inspectors
            var obj = game.SelectedObject;
            String? elementType = null;
            Guid = null;

            if(obj is LaneRange lr) {
                //Selected a road
                var road = lr.road;
                Guid = road.Guid;
                elementType = "Road strip";
                Description = $"Inspect objects.";
            }
            if (obj is LaneStrip ls) {
                //Selected a road
                var road = ls.road;
                Guid = road.Guid;
                elementType = "Road strip";
                Description = $"Inspect objects.";
            }
            if (obj is LaneEnd le) {
                //Selected a road
                var node = le.lane.RoadNode;
                Guid = node.Guid;
                elementType = "Road node";
                Description = $"Inspect objects.";
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Inspect objects");
            if(elementType != null) sb.Append("Object type: ").AppendLine(elementType);
            if (Guid != null) sb.Append("GUID: ").AppendLine(Guid.ToString());
            Description = sb.ToString();
        }
    }
}
