using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using MLEM.Ui;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Setting;
using TranSimCS.Worlds;
using TranSimCS.Menus;

namespace TranSimCS.Tools {
    public class GlobalSettingsTab: Panel {
        public GlobalSettingsTab(InGameMenu menu) : base(MLEM.Ui.Anchor.AutoLeft, new(1, 1), true) {
            var regenerateAllButton = new Button(Anchor.AutoLeft, new(1f, 20), "Regenerate all meshes", "Applies meshing settings and fixes stale meshes");
            regenerateAllButton.OnPressed += (e) => {
                var world = menu.World;
                foreach(var obj in world.RoadSections.data) obj.Mesh.Invalidate();
                foreach (var obj in world.RoadSegments.data) obj.Mesh.Invalidate();
            };
            AddChild(regenerateAllButton);

            AddSetting(this, "Road spline accuracy", int.Parse, x => x.ToString(), Settings.RoadAccuracyProp);

            var invertNormalsCheck = new Checkbox(Anchor.AutoLeft, new(1, 20), "Invert all normals");
            UI.AddProperty(invertNormalsCheck, Settings.InvertAllNormalsProp);
            AddChild(invertNormalsCheck);

            var showGroundCheck = new Checkbox(Anchor.AutoLeft, new(1, 20), "Show ground");
            UI.AddProperty(showGroundCheck, Settings.ShowGroundProp);
            AddChild(showGroundCheck);

            var dayNightCheck = new Checkbox(Anchor.AutoLeft, new(1, 20), "Day/night cycle");
            UI.AddProperty(dayNightCheck, Settings.DayNightCycleProp);
            AddChild(dayNightCheck);

            var spawnCarsCheck = new Checkbox(Anchor.AutoLeft, new(1, 20), "Naturally spawned cars");
            UI.AddProperty(spawnCarsCheck, Settings.SpawnCarsProp);
            AddChild(spawnCarsCheck);

            AddSetting(this, "Car spawn frequency [Hz/lane]", float.Parse, x => x.ToString(), Settings.CarSpawnRateProp);
            AddSetting(this, "Lenght of day/night cycle [s]", float.Parse, x => x.ToString(), Settings.DayTimeLengthProp);
        }

        public static TextField AddSetting<T>(Panel panel, String name, Func<string, T> fromString, Func<T, string> toString, Property<T> prop) {
            return AddSettingWithAction(panel, name, x => {
                var newValue = fromString(x);
                prop.Value = newValue;
            }, toString, prop);
        }
        public static TextField AddSettingWithAction<T>(Panel panel, String name, Action<string> setter, Func<T, string> toString, Property<T> prop) {
            var label = new Paragraph(Anchor.AutoLeft, 0.5f, name);
            panel.AddChild(label);

            var textField = new TextField(Anchor.AutoInline, new(0.5f, 20));
            textField.AddTooltip("Enter to confirm. RMB to cancel. Cancels when the property is changed");
            panel.AddChild(textField);

            void Revert() => textField.SetText(toString(prop.Value));

            textField.OnEnterPressed = (e) => {
                //Confirm
                try {
                    setter(textField.Text);
                } catch {
                    Revert();
                }
            };

            //When RMB is pressed, revert the value
            textField.OnSecondaryPressed = (e) => Revert();
            prop.ValueChanged += (s, e) => Revert();
            Revert();
            return textField;
        }

    }
}
