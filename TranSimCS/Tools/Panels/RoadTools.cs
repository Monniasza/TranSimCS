using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MLEM.Ui.Style;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Tools.Panels {
    public class RoadTools : Panel {
        public InGameMenu Game { get; private set; }
        public Checkbox flattenTilt { get; private set; }
        public Checkbox flattenIncline { get; private set; }
        public Checkbox anarchyCheck { get; private set; }
        public NumberField heightStepField { get; private set; }
        public NumberField HeightField { get; private set; }
        public Property<float> HeightStep { get; private set; }
        public Property<float> Height {  get; private set; }
        public Property<ChainMode> ChainMode { get; private set; }
        public Checkbox isYLocal { get; private set; }
        public Checkbox isInfinite { get; private set; }
        public Property<Alignment> AlignmentProp { get; private set; }


        public StyleProp<TextureRegion> LoadStyleProp(string name) {
            return new StyleProp<TextureRegion>(new TextureRegion(Game.Game.Content.Load<Texture2D>(name)));
        }

        public RoadTools(InGameMenu game)
            : base(Anchor.AutoLeft, new(1, 1), true) {
            Game = game;

            var settingsLabel = new Paragraph(Anchor.AutoInline, 0.4f, "Settings");
            AddChild(settingsLabel);
            anarchyCheck = CreateCheck("Anarchy", "ui/anarchy2", Color.Orange);
            flattenTilt = CreateCheck("Flatten tilt", "ui/flatTilt");
            flattenTilt.Checked = true;
            flattenIncline = CreateCheck("Flatten inclination", "ui/flatIncline");
            flattenIncline.Checked = true;
            isYLocal = CreateCheck("Global Y reference", "ui/ylocal");
            isYLocal.AddProperty(game.configuration.SnapGrid.IsYLocalProp);
            isInfinite = CreateCheck("Infinite snapping grid", "ui/node");
            isInfinite.AddProperty(game.configuration.SnapGrid.IsInfiniteProp);

            //Modes
            var modesLabel = new Paragraph(Anchor.AutoInlineBottom, 0.5f, "Modes");
            AddChild(modesLabel);

            CreateModeButton(new StraightMode(), "ui/line");
            var curvedButton = CreateModeButton(new CircMode(), "ui/curved");
            CreateModeButton(new SBendMode(), "ui/sbend");
            curvedButton.Checked = true;
            //CreateModeButton("ui/sbend3C", "S-bend, custom direction");

            //Reference modes (L, C, R)
            AlignmentProp = new(Alignment.Left, "alignment");
            var alignmentLabel = new Paragraph(Anchor.AutoInlineBottom, 0.5f, "Alignment of road to the cursor");
            AddChild(alignmentLabel);
            UI.CreateRadio(game, this, "Left", "ui/alignl", AlignmentProp, Alignment.Left);
            UI.CreateRadio(game, this, "Center", "ui/alignc", AlignmentProp, Alignment.Center);
            UI.CreateRadio(game, this, "Right", "ui/alignr", AlignmentProp, Alignment.Right);

            //Spec-transfer modes
            var specTransferLabel = new Paragraph(Anchor.AutoInlineBottom, 0.5f, "Lane-spec transfer method");
            AddChild(specTransferLabel);
            ChainMode = new Property<ChainMode>(RoadCreationTool.chained, "chainMode");
            UI.CreateRadio(game, this, "From previous", "ui/chain", ChainMode, RoadCreationTool.chained);
            UI.CreateRadio(game, this, "Custom from road configurator", "ui/customsettings", ChainMode, RoadCreationTool.custom);

            //Height adjustment
            HeightStep = new Property<float>(10, "heightStep");
            heightStepField = UI.SetUpFloatProp("Height step [m]", this, HeightStep);
            Height = new Property<float>(0.1f, "height");
            HeightField = UI.SetUpFloatProp("Height [m]", this, Height);
        }

        public Checkbox CreateCheck(string name, string icon, Color? checkColor = null, Color? uncheckColor = null) {
            return UI.CreateCheck(Game, this, name, icon, checkColor, uncheckColor);
        }

        public RadioButton CreateModeButton(RoadMode mode, string icon) {
            RadioButton radio = new RadioButton(Anchor.AutoInline, new(21, 21), "", false, "mode");
            radio.Checkmark = LoadStyleProp(icon);
            radio.UncheckColor = Color.Gray;
            radio.CheckColor = Color.White;
            radio.AddTooltip((p) => mode.Name);
            radio.OnSelected += (a) => Game.RoadCreationTool.Mode = mode;
            AddChild(radio);
            return radio;
        }
    }
}
