using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;

namespace TranSimCS.Tools {
    public class SegmentTools: Panel {
        public readonly InGameMenu Menu;
        public readonly Property<int> AddRemoveLeft;
        public readonly Property<int> AddRemoveRight;
        public SegmentTools(InGameMenu menu): base(MLEM.Ui.Anchor.AutoLeft, new(1,1), true) {
            this.Menu = menu;
            this.AddRemoveLeft = new(0, "addRemoveLeft");
            this.AddRemoveRight = new(0, "addRemoveRight");

            GlobalSettingsTab.AddSetting(this, "Add or remove lanes on the left", int.Parse, x => x.ToString(), AddRemoveLeft);
            GlobalSettingsTab.AddSetting(this, "Add or remove lanes on the right", int.Parse, x => x.ToString(), AddRemoveRight);
        }
    }
}
