using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class PrecPos: ITool {
        public PrecPos(InGameMenu menu) {

        }

        string ITool.Name => throw new NotImplementedException();

        string ITool.Description => throw new NotImplementedException();

        void ITool.Draw(GameTime gameTime) {
            throw new NotImplementedException();
        }

        void ITool.Draw2D(GameTime gameTime) {
            throw new NotImplementedException();
        }

        (object[], string)[] ITool.PromptKeys() {
            return [
                ([MouseButton.Left], "Pick an object"),
                ([MouseButton.Right], "Unselect an object"),
                ([MouseButton.Middle], "Copy position and orientation"),
                ([Keys.D1], "Set length step to 50mm"),
                ([Keys.D2], "Set length step to 100mm"),
                ([Keys.D3], "Set length step to 1m"),
                ([Keys.D4], "Set length step to 10m"),
                ([Keys.D5], "Set length step to 100m"),
                ([Keys.D6], "Set length step to 1km"),
                ([Keys.D7], "Set length step to 1.5m"),
                ([Keys.D8], "Set length step to 2m"),
                ([Keys.D9], "Set length step to 3m"),
                ([Keys.D0], "Set length step to 4m"),
                ([Keys.Q], "Set angle step to 1 deg"),
                ([Keys.W], "Set angle step to 5 deg"),
                ([Keys.E], "Set angle step to 10 deg"),
                ([Keys.R], "Set angle step to 15 deg"),
                ([Keys.T], "Set angle step to 22.5 deg"),
                ([Keys.Y], "Set angle step to 30 deg"),
                ([Keys.U], "Set angle step to 45 deg"),
                ([Keys.I], "Set angle step to 60 deg"),
                ([Keys.O], "Set angle step to 90 deg"),
                ([Keys.P], "Set angle step to 18 deg"),
                ([Keys.OemOpenBrackets], "Set angle step to 36 deg"),
                ([Keys.OemCloseBrackets], "Set angle step to 72 deg"),
                ([Keys.A], "X+"), ([Keys.Z], "X-"),
                ([Keys.S], "Y+"), ([Keys.X], "X-"),
                ([Keys.D], "Z+"), ([Keys.C], "X-"),
                ([Keys.F], "AZ+"), ([Keys.V], "AZ-"),
                ([Keys.G], "INC+"), ([Keys.B], "INC-"),
                ([Keys.H], "TLT+"), ([Keys.N], "TLT-"),
                ([Keys.J], "rZ+"), ([Keys.M], "rZ-"),
                ([Keys.K], "rX+"), ([Keys.OemComma], "rX-"),
                ([Keys.L], "rY+"), ([Keys.OemPeriod], "rY-"),
                ([Keys.OemSemicolon], "Paste orientation"), ([Keys.OemQuotes], "Paste position"),
                ([Keys.OemQuestion], "Paste azimuth"),
                ];
        }

        void ITool.Update(GameTime gameTime) {
            throw new NotImplementedException();
        }
    }
}
