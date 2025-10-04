using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Font;
using MLEM.Ui;
using MLEM.Ui.Elements;

namespace TranSimCS.Menus {
    public delegate void NumberCallback(NumberField field, float value);

    public class NumberField : TextField {
        public event NumberCallback? ValueChanged;

        private float lastValidNumber;
        public float Value {
            get => lastValidNumber;
            set {
                if (lastValidNumber == value) return;
                lastValidNumber = value;
                SetText(value);
            }
        }

        public NumberField(Anchor anchor, Vector2 size, GenericFont? font = null, float value = 0)
            : base(anchor, size, TextField.OnlyNumbers, font, value.ToString()) {
            lastValidNumber = value;

            OnTextChange += (field, text) => {
                var isSuccessful = float.TryParse(text, NumberStyles.Any, null, out var number);
                if (isSuccessful) lastValidNumber = number;
                ValueChanged?.Invoke(this, number);
            };
        }
    }
}
