using System;
using LanguageExt.ClassInstances.Pred;
using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Roads;

namespace TranSimCS.Tools.Panels {
    public class FinishTools: Panel{
        private Property<RoadFinish> Finish;
        private NumberField heightField;
        private NumberField angleField;
        private EnumDropdown<Surface> surfaceDropdown;

        public FinishTools(InGameMenu menu): base(MLEM.Ui.Anchor.CenterLeft, new (1, 1), true) {
            var finish = menu.configuration.RoadFinishProp;
            Finish = finish;

            GlobalSettingsTab.AddSettingWithAction(this, "Height [m]", x => {
                var finish = Finish.Value;
                finish.depth = float.Parse(x);
                Finish.Value = finish;
            }, rf => rf.depth.ToString(), Finish);
            GlobalSettingsTab.AddSettingWithAction(this, "Angle [degs]", x => {
                var finish = Finish.Value;
                finish.angle = MathHelper.ToRadians(float.Parse(x));
                Finish.Value = finish;
            }, rf => MathHelper.ToDegrees(rf.angle).ToString(), Finish);

            Paragraph surfaceParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Surface texture");
            AddChild(surfaceParagraph);
            surfaceDropdown = new(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), Finish.Value.subsurface);
            surfaceDropdown.SelectedValueProp.ValueChanged += Surface_ValueChanged;
            AddChild(surfaceDropdown);

            Finish.ValueChanged += FinishProp_ValueChanged;
        }

        private void FinishProp_ValueChanged(object? sender, PropertyChangedEventArgs2<RoadFinish> e) {
            surfaceDropdown.SelectedValue = e.NewValue.subsurface;
        }

        private void Surface_ValueChanged(object? sender, PropertyChangedEventArgs2<Surface> e) {
            var finish = Finish.Value;
            finish.subsurface = e.NewValue;
            Finish.Value = finish;
        }
    }
}
