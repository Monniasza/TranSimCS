using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;

namespace TranSimCS.Tools {
    public class CarLauncherTab: Panel {
        public readonly Property<float> CarVelocityProp;
        public float CarVelocity { get => CarVelocityProp.Value; set => CarVelocityProp.Value = value; }

        public CarLauncherTab(InGameMenu menu): base(MLEM.Ui.Anchor.AutoLeft, new(1,1), true) {
            CarVelocityProp = new(25, "carVelocity");

            GlobalSettingsTab.AddSetting(this, "Car velocity [m/s]", float.Parse, x => x.ToString(), CarVelocityProp);
        }
    }
}
