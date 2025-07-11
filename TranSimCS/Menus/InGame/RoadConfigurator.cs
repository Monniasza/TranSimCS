using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Menus.InGame {
    public class RoadConfigurator : Panel {
        public readonly Property<LaneSpec> laneSpecProp;
        private Panel indicator;
        public readonly Checkbox[] checks;
        public TextField inR, inG, inB, inA;
        public RoadConfigurator(InGameMenu menu, Property<LaneSpec> laneSpecProp, Anchor anchor, Vector2 size, bool setHeightBasedOnChildren = false, bool scrollOverflow = false, bool autoHideScrollbar = true) : base(anchor, size, setHeightBasedOnChildren, scrollOverflow, autoHideScrollbar) {
            this.laneSpecProp = laneSpecProp;
            inR = SetUpChannel("Red: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.R = GetNewValue(s, color.R);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inG = SetUpChannel("Green: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.G = GetNewValue(s, color.G);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inB = SetUpChannel("Blue: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.B = GetNewValue(s, color.B);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inA = SetUpChannel("Alpha: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.A = GetNewValue(s, color.A);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            }, "255");

            indicator = new Panel(Anchor.AutoLeft, new(200, 50));
            AddChild(indicator);

            //Vehicle types
            var vehicleTypes = new string[] {
                "Car", "Truck", "Bus", "Bike", "Pedestrian",
                "Light rail", "Heavy rail", "Equestrians", "Airplanes", "Rockets"
            };

            checks = new Checkbox[vehicleTypes.Length];
            for(int i = 0; i <  vehicleTypes.Length; i++) {
                var vehicleName = vehicleTypes[i];
                var vehicleType = (VehicleTypes)(1 << i);
                var check = new Checkbox((i == 0 ? Anchor.AutoLeft : Anchor.AutoInline), new(100, 20), vehicleName, false);
                check.Checked = (vehicleType & laneSpecProp.Value.VehicleTypes) != VehicleTypes.None;
                check.OnCheckStateChange += (element, ev) => SetVehicleTypeProperty(i, check.Checked);
                checks[i] = check;
                AddChild(check);
            }

            (string, VehicleTypes)[] compounds = [
                ("Clear all", VehicleTypes.None),
                ("Motor vehicles", VehicleTypes.MotorVehicles),
                ("Non-motorized vehicles and pedestrians", VehicleTypes.Path),
                ("Vehicles", VehicleTypes.Vehicles),
                ("Aircraft", VehicleTypes.Aircraft),
                ("All vehicles", VehicleTypes.Vehicles),
                ("All traffic", VehicleTypes.Transport),
                ("Everything", VehicleTypes.All)
            ];

            var style = new UiStyle(menu.Game.defaultUiStyle);
            var styleProp = new StyleProp<UiStyle>(style);
            indicator.Style = styleProp;
            OnChange(this, null);
            laneSpecProp.ValueChanged += OnChange;
        }

        private void SetVehicleTypeProperty(int offset, bool value) {
            var laneSpec = laneSpecProp.Value;
            var vehicles = laneSpec.VehicleTypes;
            VehicleTypes flag = (VehicleTypes)(1 << offset);
            VehicleTypes negFlag = ~flag;
            vehicles = (vehicles & negFlag) | (value ? flag : VehicleTypes.None);
            laneSpec.VehicleTypes = vehicles;
            laneSpecProp.Value = laneSpec;
        }
        private TextField SetUpChannel(string title, Action<string> action, string defaultValue = "128") {
            var textfieldSize = new Vector2(100, 20);
            Paragraph labelRed = new Paragraph(Anchor.AutoLeft, 100, title);
            AddChild(labelRed);
            TextField inRed = new TextField(Anchor.AutoInline, textfieldSize, null, null, defaultValue);
            AddChild(inRed);
            inRed.OnTextChange = (field, str) => action(str);
            action(defaultValue);
            return inRed;
        }
        private byte GetNewValue(string s, byte oldValue) {
            if(s.Length == 0) return 0;
            if(byte.TryParse(s, out var value)) { return value; }
            return oldValue;
        }
        private void OnChange(object sender, EventArgs e) {
            var style = indicator.Style.Value;
            var color = laneSpecProp.Value.Color;
            style.PanelColor = color;
            indicator.Style = new StyleProp<UiStyle>(style);
            inR.SetText(color.R.ToString());
            inG.SetText(color.G.ToString());
            inB.SetText(color.B.ToString());
            inA.SetText(color.A.ToString());

            for(int i = 0; i < checks.Length; i++) {
                VehicleTypes flag = (VehicleTypes)(1 << i);
                checks[i].Checked = (laneSpecProp.Value.VehicleTypes & flag) != VehicleTypes.None;
            }
        }
    }
}
