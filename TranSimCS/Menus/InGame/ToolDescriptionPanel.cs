using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;

namespace TranSimCS.Menus.InGame {
    public class ToolDescriptionPanel: Panel {
        private readonly Paragraph ToolName;
        private readonly Paragraph ToolDescription;
        private readonly InGameMenu Menu;

        public ToolDescriptionPanel(InGameMenu menu):
            base(MLEM.Ui.Anchor.TopLeft, new(0.5f, 20), true) {
            Menu = menu;
            ToolName = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 1, "");
            ToolName.RegularFont = menu.Game.Gsf;
            AddChild( ToolName );
            ToolDescription = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 1, "");
            AddChild( ToolDescription );
        }

        public void Update() {
            var Tool = Menu.configuration.Tool;
            ToolName.Text = (Tool?.Name) ?? "no tool";
            ToolName.SetAreaDirty();
            ToolDescription.Text = (Tool?.Description) ?? "";
            ToolDescription.SetAreaDirty();
        }
    }
}
