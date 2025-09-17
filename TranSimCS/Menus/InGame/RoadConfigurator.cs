using System;
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

            //Color selector
            var colorsPanel = new Panel(Anchor.AutoInline, new(0.25f, 0.5f));
            AddChild(colorsPanel);
            inR = SetUpProp("Red: ", colorsPanel, ls => ls.Color.R.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.R = GetNewValue(s, color.R);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inG = SetUpProp("Green: ", colorsPanel, ls => ls.Color.G.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.G = GetNewValue(s, color.G);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inB = SetUpProp("Blue: ", colorsPanel, ls => ls.Color.B.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.B = GetNewValue(s, color.B);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inA = SetUpProp("Alpha: ", colorsPanel, ls => ls.Color.A.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.A = GetNewValue(s, color.A);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });

            indicator = new Panel(Anchor.AutoLeft, new(200, 50));
            colorsPanel.AddChild(indicator);

            //Vehicle types
            var vehicleTypes = new string[] {
                "Car", "Truck", "Bus", "Bike", "Pedestrian",
                "Light rail", "Heavy rail", "Equestrians", "Airplanes", "Rockets"
            };
            checks = new Checkbox[vehicleTypes.Length];
            var checksPanel = new Panel(Anchor.AutoInline, new(0.25f, 0.5f));
            AddChild(checksPanel);
            for(int i = 0; i <  vehicleTypes.Length; i++) {
                var x = i;
                var vehicleName = vehicleTypes[x];
                var vehicleType = (VehicleTypes)(1 << x);
                var check = new Checkbox(Anchor.AutoInline, new(100, 20), vehicleName, false);
                check.Checked = (vehicleType & laneSpecProp.Value.VehicleTypes) != VehicleTypes.None;
                check.OnCheckStateChange += (element, ev) => SetVehicleTypeProperty(x, check.Checked);
                checks[x] = check;
                checksPanel.AddChild(check);
            }

            //Vehicle compound types
            var compoundsPanel = new Panel(Anchor.AutoInline, new(0.25f, 0.5f));
            AddChild(compoundsPanel);
            (string, VehicleTypes)[] compounds = [
                ("Clear all", VehicleTypes.None),
                ("Motor vehicles", VehicleTypes.MotorVehicles),
                ("Non-motorized traffic", VehicleTypes.Path),
                ("Vehicles", VehicleTypes.Vehicles),
                ("Aircraft", VehicleTypes.Aircraft),
                ("All road vehicles", VehicleTypes.Vehicles),
                ("All road traffic", VehicleTypes.Transport),
                ("Railway", VehicleTypes.Rail),
                ("Everything", VehicleTypes.All)
            ];
            foreach (var compound in compounds) {
                var button = new Button(Anchor.AutoInline, new(100, 40), compound.Item1);
                button.OnPressed += (s) => {
                    var laneSpec = laneSpecProp.Value;
                    laneSpec.VehicleTypes = compound.Item2;
                    laneSpecProp.Value = laneSpec;
                };
                compoundsPanel.AddChild(button);
            }

            //Vehicle presets
            (string, LaneSpec)[] presets = [
                ("Default on road", LaneSpec.Default),
                ("Bike lane", LaneSpec.Bicycle),
                ("Sidewalk", LaneSpec.Pedestrian),
                ("Path", LaneSpec.Path),
                ("Motorway", LaneSpec.Motorway),
                ("Bus lane", LaneSpec.Bus),
                ("Platform", LaneSpec.Platform),
                ("Empty", LaneSpec.None)
            ];
            var presetsPanel = new Panel(Anchor.AutoInline, new(0.25f, 0.5f));
            AddChild(presetsPanel);
            foreach (var preset in presets) {
                var button = new Button(Anchor.AutoInline, new(100, 20), preset.Item1);
                button.OnPressed += (s) => laneSpecProp.Value = preset.Item2;
                presetsPanel.AddChild(button);
            }

            //Geometric presets
            var specsPanel = new Panel(Anchor.AutoInline, new(0.25f, 0.5f));
            AddChild(specsPanel);
            SetUpProp("Width [m]", specsPanel, ls => ls.Width.ToString(), str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.Width = GetNewFloat(str, laneSpec.Width);
                laneSpecProp.Value = laneSpec;
            });
            SetUpProp("Speed limit [km/h]", specsPanel, ls => ls.SpeedLimit.ToString(), str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.SpeedLimit = GetNewFloat(str, laneSpec.SpeedLimit);
                laneSpecProp.Value = laneSpec;
            });


            var style = new UiStyle(menu.Game.defaultUiStyle);
            var styleProp = new StyleProp<UiStyle>(style);
            indicator.Style = styleProp;
            UpdateValues(laneSpecProp.Value);
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
        private TextField SetUpProp(string title, Panel panel, Func<LaneSpec, string> getter, Action<String> setter) {
            var textfieldSize = new Vector2(100, 20);
            Paragraph label = new Paragraph(Anchor.AutoLeft, 100, title);
            panel.AddChild(label);
            TextField textfield = new TextField(Anchor.AutoInline, textfieldSize, null, null, getter(laneSpecProp.Value));
            panel.AddChild(textfield);
            textfield.OnTextChange = (field, str) => setter(str);
            OnValuesChanged += (ls) => textfield.SetText(getter(ls));
            return textfield;
        }
        private byte GetNewValue(string s, byte oldValue) {
            if(s.Length == 0) return 0;
            if(byte.TryParse(s, out var value)) { return value; }
            return oldValue;
        }
        public static float GetNewFloat(string s, float oldValue) {
            if (s.Length == 0) return 0;
            if (float.TryParse(s, out var value)) { return value; }
            return oldValue;
        }
        private void OnChange(object sender, PropertyChangedEventArgs2<LaneSpec> e) => UpdateValues(e.NewValue);

        private event Action<LaneSpec> OnValuesChanged;
        private void UpdateValues(LaneSpec laneSpec) {
            var style = indicator.Style.Value;
            var color = laneSpec.Color;
            style.PanelColor = color;
            indicator.Style = new StyleProp<UiStyle>(style);
            OnValuesChanged?.Invoke(laneSpec);

            for (int i = 0; i < checks.Length; i++) {
                VehicleTypes flag = (VehicleTypes)(1 << i);
                checks[i].Checked = (laneSpec.VehicleTypes & flag) != VehicleTypes.None;
            }
        }
    }
}
