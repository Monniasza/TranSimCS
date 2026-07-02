using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MLEM.Ui.Elements;
using TranSimCS.Geometry;
using TranSimCS.Menus;
using TranSimCS.Menus.InGame;
using TranSimCS.Property;
using TranSimCS.Worlds;

namespace TranSimCS.Tools {
    public class PrecPosTools : Panel {
        public readonly Property<float> heightProp;
        public readonly NumberField heightIncrement;
        public readonly Property<float> posProp;
        public readonly NumberField posIncrement;
        public readonly Property<float> yawProp;
        public readonly NumberField yawIncrement;
        public readonly Property<float> tiltProp;
        public readonly NumberField tiltIncrement;

        //VALUES
        public readonly Property<IProperty<PositionEulerAngles>> movedObjectRef;
        public readonly ChangeableBackedProperty<PositionEulerAngles> prop;
        public readonly TextField x;
        public readonly TextField y;
        public readonly TextField z;
        public readonly TextField yaw;
        public readonly TextField pitch;
        public readonly TextField roll;

        public PrecPosTools(InGameMenu menu) : base(MLEM.Ui.Anchor.AutoLeft, new(1, 1), true) {
            //Set up properties
            movedObjectRef = new(null, "objref");
            prop = new("pos", movedObjectRef);

            //Set up fields
            heightProp = new(1, "incpY");
            heightIncrement = UI.SetUpFloatProp("Height increment", this, heightProp);
            posProp = new(4, "incpXZ");
            posIncrement = UI.SetUpFloatProp("Horizontal increment", this, posProp);
            yawProp = new(30, "incrY");
            yawIncrement = UI.SetUpFloatProp("Yaw increment", this, yawProp);
            tiltProp = new(1, "incpXZ");
            tiltIncrement = UI.SetUpFloatProp("Tilt increment", this, tiltProp);

            //Set up positioning fields
            x = GlobalSettingsTab.AddSettingWithAction(this, "X position (W-E) [m]", x => {
                var P = prop.Value;
                P.Position.X = float.Parse(x);
                prop.Value = P;
            }, x => x.Position.X.ToString(), prop);
            y = GlobalSettingsTab.AddSettingWithAction(this, "Y position (height above ground) [m]", x => {
                var P = prop.Value;
                P.Position.Y = float.Parse(x);
                prop.Value = P;
            }, x => x.Position.Y.ToString(), prop);
            z = GlobalSettingsTab.AddSettingWithAction(this, "Z position (S - N) [m]", x => {
                var P = prop.Value;
                P.Position.Z = float.Parse(x);
                prop.Value = P;
            }, x => x.Position.Z.ToString(), prop);
            yaw = GlobalSettingsTab.AddSettingWithAction(this, "Azimuth (forward vs north) [degs]", x => {
                var P = prop.Value;
                P.Azimuth = GeometryUtils.DegsToField(float.Parse(x));
                prop.Value = P;
            }, x => GeometryUtils.FieldToDegs(x.Azimuth).ToString(), prop);
            pitch = GlobalSettingsTab.AddSettingWithAction(this, "Pitch (forward vs horizontal) [degs]", x => {
                var P = prop.Value;
                P.Inclination = MathHelper.ToRadians(float.Parse(x));
                prop.Value = P;
            }, x => MathHelper.ToDegrees(x.Inclination).ToString(), prop);
            roll = GlobalSettingsTab.AddSettingWithAction(this, "Roll (lateral vs horizontal) [degs]", x => {
                var P = prop.Value;
                P.Tilt = MathHelper.ToRadians(float.Parse(x));
                prop.Value = P;
            }, x => MathHelper.ToDegrees(x.Tilt).ToString(), prop);
        }
    }
}
