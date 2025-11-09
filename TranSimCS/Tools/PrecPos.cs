using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Geometry;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads.Node;
using TranSimCS.Tools.Panels;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Tools {
    public class PrecPos: ITool {

        public readonly InGameMenu menu;
        public readonly PrecPosTools tools;
        public readonly Property<IPosition?> selectionProp;
        public IPosition? Selection { get => selectionProp.Value; set => selectionProp.Value = value; }

        public PrecPos(InGameMenu menu) {
            this.menu = menu;
            this.tools = menu.ToolsPanel.GetPanel<PrecPosTools>(ToolAttribs.showPosManip);
            this.selectionProp = new(null, "sel", null, Equality.ReferenceEqualComparer<IPosition?>());

            selectionProp.ValueChanged += SelectionProp_ValueChanged;
        }

        private void SelectionProp_ValueChanged(object? sender, PropertyChangedEventArgs2<IPosition?> e) {
            tools.link.B = e.NewValue?.PositionProp;
        }

        string ITool.Name => "Fine-tune positioning and orientation";

        string ITool.Description => Selection == null ? "Select an object to move." : "Use keys to move the selected object very accurately. RMB to cancel";

        void ITool.Draw(GameTime gameTime) {
            //unused
        }

        void ITool.Draw2D(GameTime gameTime) {
            //unused
        }

        (object[], string)[] ITool.PromptKeys() {
            var sh = Keys.LeftShift;
            var ct = Keys.LeftControl;
            return [
                ([sh], "Rotate"),
                ([ct], "Relative"),
                ([MouseButton.Left], "Pick an object"),
                ([MouseButton.Right], "Unselect an object"),
                ([MouseButton.Middle], "Copy position and orientation"),
                ([Keys.L], "X+"), ([Keys.J], "X-"),
                ([Keys.I], "Y+"), ([Keys.K], "Y-"),
                ([Keys.O], "Z+"), ([Keys.U], "Z-"),
                ([sh, Keys.L], "Yaw+"), ([sh, Keys.J], "Yaw-"),
                ([sh, Keys.K], "Pit+"), ([sh, Keys.I], "Pit-"),
                ([sh, Keys.O], "Rol+"), ([sh, Keys.U], "Rol-"),
                ([Keys.D0], "Set Y to 0.1 (ground)"),
                ([Keys.D1], "Set inclination to 0"),
                ([Keys.D2], "Set tilt to 0"),
                ([Keys.D3], "Set yaw to north"),
                ([Keys.OemSemicolon], "Paste orientation"), 
                ([Keys.OemQuotes], "Paste position"),
                ([Keys.OemQuestion], "Paste azimuth"),
            ];
        }

        void ITool.Update(GameTime gameTime) {
            //unused
        }

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showPosManip);
            action.Add(ToolAttribs.noShift);
        }

        void ITool.OnKeyDown(Keys key) {
            var isRelative = menu.Game.KeyboardState.IsKeyDown(Keys.LeftControl);
            var isRotation = menu.Game.KeyboardState.IsKeyDown(Keys.LeftShift);

            var yawRate = tools.yawIncrement.Value;
            var tiltRate = tools.tiltIncrement.Value;

            float roll = 0;
            float pitch = 0;
            float yaw = 0;

            if (isRotation) {
                switch (key) {
                    //Rotation
                    case Keys.J: yaw -= yawRate; break;
                    case Keys.L: yaw += yawRate; break;
                    case Keys.K: pitch += tiltRate; break;
                    case Keys.I: pitch -= tiltRate; break;
                    case Keys.U: roll -= tiltRate; break;
                    case Keys.O: roll += tiltRate; break;
                }
            } else {
                switch (key) {
                    //Movement
                    case Keys.J: Move(-Vector3.UnitX, isRelative); break;
                    case Keys.L: Move(Vector3.UnitX, isRelative); break;
                    case Keys.K: Move(-Vector3.UnitY, isRelative); break;
                    case Keys.I: Move(Vector3.UnitY, isRelative); break;
                    case Keys.U: Move(-Vector3.UnitZ, isRelative); break;
                    case Keys.O: Move(Vector3.UnitZ, isRelative); break;
                }
            }

            var rollRads = MathHelper.ToRadians(roll);
            var pitchRads = MathHelper.ToRadians(pitch);
            var yawFields = GeometryUtils.DegsToField(yaw);
            var obj = Selection;
            obj?.Rotate(yawFields, pitchRads, rollRads);
        }

        public void Move(Vector3 vec, bool rel) {
            var obj = Selection;
            if (obj == null) return;

            var xzmul = tools.posIncrement.Value;
            var ymul = tools.heightIncrement.Value;

            vec.X *= xzmul;
            vec.Y *= ymul;
            vec.Z *= xzmul;            

            if (rel) {
                var tr = obj.PositionData.CalcReferenceFrame();
                tr.O = Vector3.Zero;
                vec = tr.Transform(vec);
            }

            obj.Drag(vec, Vector3.Zero);
        }

        void ITool.OnClick(MouseButton button) {
            switch (button) {
                case MouseButton.Left:
                    var mouseOverObj = menu.SelectedObject;
                    Selection = GetPositioningFromSelection(mouseOverObj);
                    break;
                case MouseButton.Right:
                    Selection = null;
                    break;
                case MouseButton.Middle:
                    //TODO copy/paste
                    break;
            }
        }

        public static IPosition? GetPositioningFromSelection(object? selection) {
            if(selection == null) return null;
            if(selection is IPosition positioning) return positioning;
            if (selection is LaneEnd le) return le.lane.RoadNode;
            if (selection is Lane lane) return lane.RoadNode;
            return null;
        }
    }
}
