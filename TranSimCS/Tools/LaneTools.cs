using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui;
using MLEM.Ui.Elements;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Roads.Node;

namespace TranSimCS.Tools {
    public class LaneTools: Panel {
        public Property<Lane?> PickedLaneProp;
        public Property<float> SnappingIncrementProp;
        public Property<bool> SnappingEnabledProp;
        public Property<bool> SnappingIsAbsoluteProp;

        public Lane? PickedLane {
            get => PickedLaneProp.Value;
            set => PickedLaneProp.Value = value;
        }
        public float SnappingSetting {
            get => SnappingIncrementProp.Value;
            set => SnappingIncrementProp.Value = value;
        }
        public bool SnappingCheckEnabled {
            get => SnappingEnabledProp.Value;
            set => SnappingEnabledProp.Value = value;
        }
        public bool SnappingIsAbsolute {
            get => SnappingIsAbsoluteProp.Value;
            set => SnappingIsAbsoluteProp.Value = value;
        }
        public float SnappingResult {
            get => SnappingCheckEnabled ? SnappingIncrementProp.Value : 0;
            set {
                if (value != 0) {
                    SnappingCheckEnabled = true;
                    SnappingSetting = value;
                } else {
                    SnappingCheckEnabled = false;
                }
            }
        }

        public LaneTools(InGameMenu game) : base(Anchor.AutoLeft, new(1, 1), true) {
            PickedLaneProp = new(null, "lane", null);
            PickedLaneProp.ValueChanged += PickedLaneProp_ValueChanged;
            SnappingIncrementProp = new(0.25f, "increment");
            SnappingEnabledProp = new(false, "enableSnap");
            SnappingIsAbsoluteProp = new(true, "absolute");
            
            Paragraph leftLabel = new Paragraph(Anchor.AutoLeft, 0.5f, "Left border");
            Paragraph rightLabel = new Paragraph(Anchor.AutoLeft, 0.5f, "Right border");
            Paragraph widthLabel = new Paragraph(Anchor.AutoLeft, 0.5f, "Width");
            Paragraph centerLabel = new Paragraph(Anchor.AutoLeft, 0.5f, "Center position");

            Checkbox snapLabel = new Checkbox(Anchor.AutoLeft, new(0.5f, 20), "Snapping increment", false);
            snapLabel.AddTooltip("0 to disable");
            NumberField snapField = new NumberField(Anchor.AutoInline, new(0.5f, 20), null, 0.25f);

            Checkbox snapAbsolute = new Checkbox(Anchor.AutoLeft, new(0.5f, 20), "Snap to absolute", true);

            AddChild(snapLabel);
            AddChild(snapField);
            AddChild(snapAbsolute);

            SnappingIncrementProp.ValueChanged += (s, e) => snapField.Value = e.NewValue;
            SnappingEnabledProp.ValueChanged += (s, e) => snapLabel.Checked = e.NewValue;
            SnappingIsAbsoluteProp.ValueChanged += (s, e) => snapAbsolute.Checked = e.NewValue;
            snapLabel.OnCheckStateChange += (s, v) => SnappingCheckEnabled = v;
            snapField.ValueChanged += (s, v) => SnappingSetting = v;
            snapAbsolute.OnCheckStateChange += (s, v) => SnappingIsAbsolute = v;
        }

        private void PickedLaneProp_ValueChanged(object? sender, PropertyChangedEventArgs2<Lane?> e) {
            e.OldValue?.DefinitionProp.ValueChanged -= PickedLaneContentsChanged;
            e.NewValue?.DefinitionProp.ValueChanged += PickedLaneContentsChanged;
            if(e.NewValue != null) UpdateValues(e.NewValue.Definition);
        }

        private void UpdateValues(LaneNode definition) {
            throw new NotImplementedException();
        }

        private void PickedLaneContentsChanged(object? sender, PropertyChangedEventArgs2<LaneNode> e) => UpdateValues(e.NewValue);
    }
}
