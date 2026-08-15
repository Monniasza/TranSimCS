using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;

namespace TranSimCS.Tools {
    public class SplitRoadTab: Panel {
        public readonly Property<float> SplitLengthProp;
        public float SplitLength { get => SplitLengthProp.Value; set => SplitLengthProp.Value = value; }
        public SplitRoadTab(InGameMenu menu): base(MLEM.Ui.Anchor.AutoLeft, new(1,1), true) {
            SplitLengthProp = new(6, "splitLength");

            GlobalSettingsTab.AddSetting(this, "Split length [m]", float.Parse, x => x.ToString(), SplitLengthProp);
        }
    }
}
