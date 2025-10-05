using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Input;
using MLEM.Ui.Elements;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class RoadFinishTool : ITool {
        private InGameMenu menu;
        public RoadFinishTool(InGameMenu menu) {
            this.menu = menu;
            FinishProp = new Property<RoadFinish>(RoadFinish.Embankment, "finish");
            tab = new RoadFinishTab(FinishProp);
        }

        public string Name => "Edit road finishes";

        public string Description => "";

        public Property<RoadFinish> FinishProp;
        public RoadFinish Finish { get => FinishProp.Value; set => FinishProp.Value = value; }

        private RoadFinishTab tab;

        public void OnClick(MouseButton button) {
            var selectedRoadStrip = menu.MouseOverRoad?.SelectedLaneTag?.road;
            switch (button) {
                case MouseButton.Left:
                    //Set the finish
                    selectedRoadStrip?.Finish = Finish;
                    break;
                case MouseButton.Right:
                    //Pick a finish
                    if(selectedRoadStrip != null) Finish = selectedRoadStrip.Finish;
                    break;
            }
        }

        public void Draw(GameTime gameTime) {
            //unused
        }

        public void Draw2D(GameTime gameTime) {
            //unused
        }

        public (object[], string)[] PromptKeys() {
            return [
                ([MouseButton.Left], "Apply"),
                ([MouseButton.Right], "Copy")
            ];
        }

        public void Update(GameTime gameTime) {
            //unused
        }

        public void OnOpen() {
            menu.UiSystem.Add(RoadCreationTool.uiID, tab);
        }
        public void OnClose() {
            menu.UiSystem.Remove(RoadCreationTool.uiID);
        }
    }

    public class RoadFinishTab: Panel{
        private Property<RoadFinish> Finish;
        private NumberField heightField;
        private NumberField angleField;
        private EnumDropdown<Surface> surfaceDropdown;

        public RoadFinishTab(Property<RoadFinish> finish): base(MLEM.Ui.Anchor.CenterLeft, new (200, 0.25f), true) {
            this.Finish = finish;

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
