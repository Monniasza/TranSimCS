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
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Tools {
    public class InspectTool(InGameMenu game) : ITool {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public string Name => "Inspect";

        public string Description { get; private set; } = "Inspect objects";

        public Guid? Guid { get; set; }

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public void OnClick(MouseButton button) {
            if(button == MouseButton.Left) {
                string? guidString = Guid?.ToString();
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
            var obj = game.MouseOver?.SelectedObj;
            string? elementType = null;

            if(obj is RoadStrip lr) {
                //Selected a road
                elementType = "Road strip";
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Inspect objects");
            if(elementType != null) sb.Append("Object type: ").AppendLine(elementType);

            foreach (var inspector in InspectMethods.inspectors) {
                var result = inspector(obj, this);
                if (result != null) sb.AppendLine(result);
            }

            Description = sb.ToString();
        }
    }
}
