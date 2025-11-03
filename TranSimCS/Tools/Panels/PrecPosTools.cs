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
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Tools.Panels {
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
        public readonly Property<ObjPos> prop;
        public readonly LinkProps<ObjPos> link;
        public readonly NumberField x;
        public readonly NumberField y;
        public readonly NumberField z;
        public readonly NumberField yaw;
        public readonly NumberField pitch;
        public readonly NumberField roll;

        public PrecPosTools(InGameMenu menu) : base(MLEM.Ui.Anchor.CenterLeft, new(1, 1), true) {
            //Set up properties
            prop = new Property<ObjPos>(ObjPos.Zero, "pos", null);
            Resolver<ObjPos> preferB = Resolvers.PreferB<ObjPos>;
            link = new(preferB, preferB);
            link.A = prop;

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
            x = UI.SetUpReplacementField<ObjPos>("X position (W-E)", this, x => x.Position.X, (p, v) => {
                var P = p.Position;
                P.X = v;
                p.Position = P;
                return p; 
            }, prop);
            y = UI.SetUpReplacementField<ObjPos>("Y position (height)", this, x => x.Position.Y, (p, v) => {
                var P = p.Position;
                P.Y = v;
                p.Position = P;
                return p;
            }, prop);
            z = UI.SetUpReplacementField<ObjPos>("Z position (S-N)", this, x => x.Position.Z, (p, v) => {
                var P = p.Position;
                P.Z = v;
                p.Position = P;
                return p;
            }, prop);
            yaw = UI.SetUpReplacementField<ObjPos>("Azimuth", this, x => GeometryUtils.FieldToDegs(x.Azimuth), (p, v) => {
                p.Azimuth = GeometryUtils.DegsToField(v);
                return p;
            }, prop);
            
            pitch = UI.SetUpReplacementField<ObjPos>("Pitch", this, x => MathHelper.ToDegrees(x.Inclination), (p, v) => {
                p.Inclination = MathHelper.ToRadians(v);
                return p;
            }, prop);

            roll = UI.SetUpReplacementField<ObjPos>("Roll", this, x => MathHelper.ToDegrees(x.Tilt), (p, v) => {
                p.Tilt = MathHelper.ToRadians(v);
                return p;
            }, prop);
        }


    }
}
