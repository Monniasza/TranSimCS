using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;

namespace TranSimCS.Tools {
    public class AddNodeTools : Panel {
        public readonly InGameMenu Menu;
        public readonly Property<uint> LeftLaneCount;
        public readonly Property<uint> RightLaneCount;
        public readonly Property<float> MedianWidth;

        public AddNodeTools(InGameMenu menu) : base(MLEM.Ui.Anchor.AutoLeft, new(1,1), true){
            Menu = menu;
            LeftLaneCount = new(0, "leftLanes");
            RightLaneCount = new(1, "rightLanes");
            MedianWidth = new(0, "medianWidth");

            GlobalSettingsTab.AddSetting(this, "Left lanes", uint.Parse, x => x.ToString(), LeftLaneCount);
            GlobalSettingsTab.AddSetting(this, "Right lanes", uint.Parse, x => x.ToString(), RightLaneCount);
            GlobalSettingsTab.AddSetting(this, "Median width [m]", float.Parse, x => x.ToString(), MedianWidth);
        }
    }
}
