using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Tools.Panels {
    public class RoadFinishTab: Panel{
        private Property<RoadFinish> Finish;
        private NumberField heightField;
        private NumberField angleField;
        private EnumDropdown<Surface> surfaceDropdown;

        public RoadFinishTab(InGameMenu menu): base(MLEM.Ui.Anchor.CenterLeft, new (1, 1), true) {
            var finish = menu.configuration.RoadFinishProp;
            Finish = finish;

            Paragraph heightParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Height");
            AddChild(heightParagraph);
            heightField = new NumberField(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), null, Finish.Value.depth);
            heightField.ValueChanged += HeightField_ValueChanged;
            AddChild(heightField);

            Paragraph angleParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Angle (degs)");
            AddChild(angleParagraph);
            angleField = new NumberField(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), null, MathHelper.ToDegrees(Finish.Value.angle));
            angleField.ValueChanged += AngleField_ValueChanged;
            AddChild(angleField);

            Paragraph surfaceParagraph = new Paragraph(MLEM.Ui.Anchor.AutoLeft, 0.5f, "Surface texture");
            AddChild(surfaceParagraph);
            surfaceDropdown = new(MLEM.Ui.Anchor.AutoInline, new(0.5f, 20), Finish.Value.subsurface);
            surfaceDropdown.SelectedValueProp.ValueChanged += Surface_ValueChanged;
            AddChild(surfaceDropdown);



            Finish.ValueChanged += FinishProp_ValueChanged;
        }

        private void FinishProp_ValueChanged(object? sender, PropertyChangedEventArgs2<RoadFinish> e) {
            heightField.Value = e.NewValue.depth;
            angleField.Value = MathHelper.ToDegrees(e.NewValue.angle);
            surfaceDropdown.SelectedValue = e.NewValue.subsurface;
        }

        private void Surface_ValueChanged(object? sender, PropertyChangedEventArgs2<Surface> e) {
            var finish = Finish.Value;
            finish.subsurface = e.NewValue;
            Finish.Value = finish;
        }

        private void AngleField_ValueChanged(NumberField field, float value) {
            var finish = Finish.Value;
            finish.angle = MathHelper.ToRadians(value);
            Finish.Value = finish;
        }

        private void HeightField_ValueChanged(NumberField field, float value) {
            var finish = Finish.Value;
            finish.depth = value;
            Finish.Value = finish;
        }
    }
}
