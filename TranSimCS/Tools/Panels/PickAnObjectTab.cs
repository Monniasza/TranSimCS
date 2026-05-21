using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Tools.Panels {
    public class PickAnObjectTab: Panel{

        public readonly InGameMenu menu;

        private class CameraIPositionAdapter : IPosition {
            private readonly Property<Camera> CameraProp;
            public CameraIPositionAdapter(InGameMenu menu) {
                CameraProp = menu.renderManager.CameraProp;
                PositionProp = new(ObjPos.Zero, "pos");
                Camera2Pos(CameraProp.Value);
                CameraProp.ValueChanged += (s, e) => Camera2Pos(e.NewValue);
                PositionProp.ValueChanged += (s, e) => Pos2Camera(e.NewValue);
            }

            private void Camera2Pos(Camera camera) {
                var position = PositionProp.Value;
                position.Position = camera.Position;
                position.Azimuth = GeometryUtils.RadiansToField(camera.Azimuth);
                position.Inclination = camera.Elevation;
                position.Tilt = 0;
                PositionProp.Value = position;
            }
            private void Pos2Camera(ObjPos position) {
                var camera = CameraProp.Value;
                camera.Position = position.Position;
                camera.Azimuth = GeometryUtils.FieldToRadians(position.Azimuth);
                camera.Elevation = position.Inclination;
                CameraProp.Value = camera;
            }

            public Property<ObjPos> PositionProp { get; private set; }
        }

        public readonly IPosition camera;

        public PickAnObjectTab(InGameMenu menu): base(MLEM.Ui.Anchor.AutoLeft, new (1, 1), true) {
            this.menu = menu;
            this.camera = new CameraIPositionAdapter(menu);
            AddSelectionButton("Nothing", null);
            AddSelectionButton("Camera", camera);
        }

        private void AddSelectionButton(string title, IPosition? value) {
            Button button = new Button(MLEM.Ui.Anchor.AutoLeft, new(1f, 20), title);
            button.OnPressed = (e => menu.PrecPosTool.Selection = value);
            AddChild(button);
        }
    }
}
