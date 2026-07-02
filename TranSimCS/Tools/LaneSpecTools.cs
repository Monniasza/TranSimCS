using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Roads;
using TranSimCS.Setting;

namespace TranSimCS.Tools {
    public class LaneSpecTools : Panel {
        public readonly Property<LaneSpec> laneSpecProp;
        private Panel indicator;
        public readonly Checkbox[] checks;
        public TextField inR, inG, inB, inA;
        public readonly InGameMenu menu;

        public LaneSpecTools(InGameMenu menu)
            : base(Anchor.AutoLeft, new (1, 1), true) {
            laneSpecProp = menu.configuration.LaneSpecProp;
            this.menu = menu;

            //Color selector
            inR = GlobalSettingsTab.AddSettingWithAction(this, "Red: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.R = GetNewValue(s, color.R);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            }, ls => ls.Color.R.ToString(), laneSpecProp);
            inG = GlobalSettingsTab.AddSettingWithAction(this, "Green: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.G = GetNewValue(s, color.G);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            }, ls => ls.Color.G.ToString(), laneSpecProp);
            inB = GlobalSettingsTab.AddSettingWithAction(this, "Blue: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.B = GetNewValue(s, color.B);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            }, ls => ls.Color.B.ToString(), laneSpecProp);
            inA = GlobalSettingsTab.AddSettingWithAction(this, "Alpha: ", (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.A = GetNewValue(s, color.A);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            }, ls => ls.Color.A.ToString(), laneSpecProp);

            indicator = new Panel(Anchor.AutoLeft, new(1, 20));
            AddChild(indicator);

            //Vehicle types
            var vehicleTypes = new string[] {
                "Car", "Truck", "Bus", "Bike", "Pedestrian",
                "Light rail", "Heavy rail", "Equestrians", "Airplanes", "Rockets"
            };
            Paragraph vehiclesLabel = new(Anchor.AutoLeft, 1, "Vehicles:", true);
            checks = new Checkbox[vehicleTypes.Length];
            for(int i = 0; i <  vehicleTypes.Length; i++) {
                var x = i;
                var vehicleName = vehicleTypes[x];
                var vehicleType = (VehicleTypes)(1 << x);
                var check = new Checkbox(Anchor.AutoInline, new(20, 20), null, false);
                check.AddTooltip(vehicleName);
                check.Checked = (vehicleType & laneSpecProp.Value.VehicleTypes) != VehicleTypes.None;
                check.OnCheckStateChange += (element, ev) => SetVehicleTypeProperty(x, check.Checked);
                checks[x] = check;
                AddChild(check);
            }

            //Vehicle compound types
            var compoundsDropDown = new Dropdown(Anchor.AutoLeft, new(0.49f, 20), "Vehicle presets");
            AddChild(compoundsDropDown);
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
                var button = new Button(Anchor.AutoLeft, new(1, 20), compound.Item1);
                button.OnPressed += (s) => {
                    var laneSpec = laneSpecProp.Value;
                    laneSpec.VehicleTypes = compound.Item2;
                    laneSpecProp.Value = laneSpec;
                };
                compoundsDropDown.Panel.AddChild(button);
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
            var presetsDropDown = new Dropdown(Anchor.AutoInline, new(0.5f, 20), "Lane-specs");
            AddChild(presetsDropDown);
            foreach (var preset in presets) {
                var button = new Button(Anchor.AutoLeft, new(100, 20), preset.Item1);
                button.OnPressed += (s) => laneSpecProp.Value = preset.Item2;
                presetsDropDown.Panel.AddChild(button);
            }

            //Tags
            LaneSpecToolsFlag<LaneFlags>[] flagPresets = [
                new("Allow reversing & wrong way", "signs/bidirectional", null, LaneFlags.AllowReverse),
                new("Stop", "signs/stop", null, LaneFlags.Stop),
                new("Yield", "signs/yield", null, LaneFlags.Yield),
                new("Parking", "signs/parking", null, LaneFlags.Parking),
                new("Platform", "ui/bus", null, LaneFlags.Platform),
                new("Merge or expand right", "signs/mergeright", null, LaneFlags.MergeRight),
                new("Merge or expand left", "signs/mergeleft", null, LaneFlags.MergeLeft),
                new("No switching to the left", "signs/noleft", null, LaneFlags.NoLeft),
                new("No switching to the right", "signs/noright", null, LaneFlags.NoRight),
                new("Merge/Expand", "signs/expand", "signs/merge", LaneFlags.ExpandNotMerge),
            ];

            var flagsLabel = new Paragraph(Anchor.AutoLeft, 0.5f, "Lane settings");
            AddChild(flagsLabel);
            var reverseLaneSpecButton = new Button(Anchor.AutoInline, new(0.5f, 20), "Reverse");
            reverseLaneSpecButton.OnPressed += s => laneSpecProp.Value = laneSpecProp.Value.Reverse();
            AddChild(reverseLaneSpecButton);
            foreach(var row in flagPresets) {
                var title = row.Title;
                var texture = row.Texture;
                var secondaryTexture = row.SecondaryTexture;
                var flag = row.Flag;
                var secondaryColor = (secondaryTexture == null) ? Color.Gray : Color.White;
                secondaryTexture ??= texture;

                var primaryBitmap = new TextureRegion(menu.Game.Content.Load<Texture2D>(texture));
                var secondaryBitmap = new TextureRegion(menu.Game.Content.Load<Texture2D>(secondaryTexture));
                var check = new Checkbox(Anchor.AutoInline, new(20, 20), "");
                check.UncheckColor = secondaryColor;
                void UpdateCheck() {
                    bool checced = check.Checked;
                    check.Checkmark = checced ? primaryBitmap : secondaryBitmap;
                    var newSpec = laneSpecProp.Value;
                    var newFlags = newSpec.Flags;
                    if (checced) newFlags |= flag; else newFlags &= ~flag;
                    newSpec.Flags = newFlags;
                    laneSpecProp.Value = newSpec;
                }
                check.OnCheckStateChange += (s, e) => UpdateCheck();
                laneSpecProp.ValueChanged += (s, e) => {
                    var newState = (e.NewValue.Flags & flag) != 0;
                    if(newState != check.Checked)
                        check.Checked = newState;
                };
                UpdateCheck();
                check.AddTooltip(title);
                AddChild(check);
            }

            //Geometric presets
            GlobalSettingsTab.AddSettingWithAction(this, "Width [m]", str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.Width = GetNewFloat(str, laneSpec.Width);
                laneSpecProp.Value = laneSpec;
            }, ls => ls.Width.ToString(), laneSpecProp);
            GlobalSettingsTab.AddSettingWithAction(this, "Speed limit [km/h]", str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.SpeedLimit = GetNewFloat(str, laneSpec.SpeedLimit);
                laneSpecProp.Value = laneSpec;
            }, ls => ls.SpeedLimit.ToString(), laneSpecProp);


            var style = new UiStyle(menu.Game.DefaultUiStyle);
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
            vehicles = vehicles & negFlag | (value ? flag : VehicleTypes.None);
            laneSpec.VehicleTypes = vehicles;
            laneSpecProp.Value = laneSpec;
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
