using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;

namespace TranSimCS.Tools {
    public class SegmentTools: Panel {
        public readonly InGameMenu Menu;
        public readonly Property<int> AddRemoveLeft;
        public readonly Property<int> AddRemoveRight;
        public readonly Property<uint> IncludeExcludeLeft;
        public readonly Property<uint> IncludeExcludeRight;
        public readonly Property<bool> IsInclusive;

        public Presets CurrentPresets {
            get => new Presets() {
                AddRemoveLeft = AddRemoveLeft.Value,
                AddRemoveRight = AddRemoveRight.Value,
                IncludeExcludeLeft = IncludeExcludeLeft.Value,
                IncludeExcludeRight = IncludeExcludeRight.Value,
                IsInclusive = IsInclusive.Value,
            }; set {
                AddRemoveLeft.Value = value.AddRemoveLeft;
                AddRemoveRight.Value = value.AddRemoveRight;
                IncludeExcludeLeft.Value = value.IncludeExcludeLeft;
                IncludeExcludeRight.Value = value.IncludeExcludeRight;
                IsInclusive.Value = value.IsInclusive;
            }
        }

        public struct Presets : IEquatable<Presets> {
            public int AddRemoveLeft;
            public int AddRemoveRight;
            public uint IncludeExcludeLeft;
            public uint IncludeExcludeRight;
            public bool IsInclusive;
            public override bool Equals(object? obj) {
                return obj is Presets presets && Equals(presets);
            }

            public bool Equals(Presets other) {
                return AddRemoveLeft == other.AddRemoveLeft &&
                       AddRemoveRight == other.AddRemoveRight &&
                       IncludeExcludeLeft == other.IncludeExcludeLeft &&
                       IncludeExcludeRight == other.IncludeExcludeRight &&
                       IsInclusive == other.IsInclusive;
            }

            public override int GetHashCode() {
                return HashCode.Combine(AddRemoveLeft, AddRemoveRight, IncludeExcludeLeft, IncludeExcludeRight, IsInclusive);
            }

            public static bool operator ==(Presets left, Presets right) {
                return left.Equals(right);
            }

            public static bool operator !=(Presets left, Presets right) {
                return !(left == right);
            }
        }
        public SegmentTools(InGameMenu menu): base(MLEM.Ui.Anchor.AutoLeft, new(1,1), true) {
            this.Menu = menu;
            this.AddRemoveLeft = new(0, "addRemoveLeft");
            this.AddRemoveRight = new(0, "addRemoveRight");
            this.IncludeExcludeLeft = new(0, "includeExcludeLeft");
            this.IncludeExcludeRight = new(0, "includeExcludeRight");
            this.IsInclusive = new(false, "isInclusive");
            IsInclusive.ValueChanged += IsInclusive_ValueChanged;

            Paragraph addRemoveParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 1, "Lanes to merge or expand");
            addRemoveParagraph.AddTooltip("""
                Positive for expand, negative for merge.
                From the left, [Q] to expand and [E] to merge.
                From the right, [O] to merge and [P] to expand.
                """);

            GlobalSettingsTab.AddSetting(this, null, int.Parse, x => x.ToString(), AddRemoveLeft);
            GlobalSettingsTab.AddSetting(this, null, int.Parse, x => x.ToString(), AddRemoveRight);

            //include/skip buttons
            Paragraph skipIncludeParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 1, "Lanes to include or exclude\nHOVER FOR MORE INFO");
            skipIncludeParagraph.AddTooltip($"""
                If the checkbox is checked, the numbers are lanes to include on either side.
                Otherwise, the numbers are lanes to cut from either side.
                Move the left border left with [Z] and right with [C].
                Move the right border left with [,] and right with [.].
                Swap the counts with [/]
                If there are more lanes to include than there are, all lanes on an appropriate side will be included.
                If there are more lanes to cut than there are, no lanes will be cut.
                Tip: Decrement below 0 to get an overflow and include all lanes if you're on include mode. The actual amount is {uint.MaxValue}.
                It can be reversed. If you increment past {uint.MaxValue}, you will get back to 0.
                When a road strip is places, the counts below will be reset
                """);
            AddChild(skipIncludeParagraph);

            Checkbox inclusiveCheck = new(MLEM.Ui.Anchor.AutoLeft, new(1, 20), "Are counts inclusive?");
            inclusiveCheck.AddTooltip("""
                If this checkbox is checked, the counts below are number of lanes to include on either side of the source lane.
                Otherwise the counts below are number of lanes to exclude from either side.
                Toggle with [X]. If this checkbox is changed, the counts below are reset to 0.
                """);
            UI.AddProperty(inclusiveCheck, IsInclusive);
            AddChild(inclusiveCheck);
            
            GlobalSettingsTab.AddSetting(this, null, uint.Parse, x => x.ToString(), IncludeExcludeLeft);
            GlobalSettingsTab.AddSetting(this, null, uint.Parse, x => x.ToString(), IncludeExcludeRight);
        }

        private void IsInclusive_ValueChanged(object? sender, bool old, bool val) => IncludeExcludeLeft.Value = IncludeExcludeRight.Value = 0;
    }
}
