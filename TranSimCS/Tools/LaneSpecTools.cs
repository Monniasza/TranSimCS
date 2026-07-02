using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
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
        public LaneSpecTools(InGameMenu menu)
            : base(Anchor.AutoLeft, new (1, 1), true) {
            laneSpecProp = menu.configuration.LaneSpecProp;

            //Color selector
            inR = SetUpProp("Red: ", this, ls => ls.Color.R.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.R = GetNewValue(s, color.R);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inG = SetUpProp("Green: ", this, ls => ls.Color.G.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.G = GetNewValue(s, color.G);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inB = SetUpProp("Blue: ", this, ls => ls.Color.B.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.B = GetNewValue(s, color.B);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });
            inA = SetUpProp("Alpha: ", this, ls => ls.Color.A.ToString(), (s) => {
                var laneSpec = laneSpecProp.Value;
                var color = laneSpec.Color;
                color.A = GetNewValue(s, color.A);
                laneSpec.Color = color;
                laneSpecProp.Value = laneSpec;
            });

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
            var flagPresets = new (string Title, string Texture, string? SecondaryTexture, LaneFlags Flag)[]{
                ("Allow reversing & wrong way", "signs/bidirectional", null, LaneFlags.AllowReverse),
                ("Stop", "signs/stop", null, LaneFlags.Stop),
                ("Yield", "signs/yield", null, LaneFlags.Yield),
                ("Parking", "signs/parking", null, LaneFlags.Parking),
                ("Platform", "ui/bus", null, LaneFlags.Parking),
                ("Merge or expand right", "signs/mergeright", null, LaneFlags.MergeRight),
                ("Merge or expand left", "signs/mergeleft", null, LaneFlags.MergeLeft),
                ("No switching to the left", "signs/noleft", null, LaneFlags.NoLeft),
                ("No switching to the right", "signs/noright", null, LaneFlags.NoRight),
                ("Merge/Expand", "signs/expand", "signs/merge", LaneFlags.ExpandNotMerge),
            };
            var flagsLabel = new Paragraph(Anchor.AutoLeft, 1, "Lane settings");
            AddChild(flagsLabel);
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
                check.Checkmark = primaryBitmap;
                check.UncheckColor = secondaryColor;
                check.OnCheckStateChange += (s, e) => {
                    check.Checkmark = e ? primaryBitmap : secondaryBitmap;
                    var newSpec = laneSpecProp.Value;
                    var newFlags = newSpec.Flags;
                    if (e) newFlags |= flag; else newFlags &= ~flag;
                    newSpec.Flags = flag;
                    laneSpecProp.Value = newSpec;
                };
                laneSpecProp.ValueChanged += (s, e) => {
                    var newState = (e.NewValue.Flags & flag) != 0;
                    if(newState != check.Checked)
                        check.Checked = newState;
                };
                check.AddTooltip(title);
                AddChild(check);
            }

            //Geometric presets
            SetUpProp("Width [m]", this, ls => ls.Width.ToString(), str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.Width = GetNewFloat(str, laneSpec.Width);
                laneSpecProp.Value = laneSpec;
            });
            SetUpProp("Speed limit [km/h]", this, ls => ls.SpeedLimit.ToString(), str => {
                var laneSpec = laneSpecProp.Value;
                laneSpec.SpeedLimit = GetNewFloat(str, laneSpec.SpeedLimit);
                laneSpecProp.Value = laneSpec;
            });


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
        private TextField SetUpProp(string title, Panel panel, Func<LaneSpec, string> getter, Action<string> setter) {
            return GlobalSettingsTab.AddSettingWithAction(panel, title, setter, getter, laneSpecProp);
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
