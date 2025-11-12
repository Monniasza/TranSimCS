using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using TranSimCS.Tools;
using TranSimCS.Worlds;
using TranSimCS.Worlds.Property;

namespace TranSimCS.Menus.InGame {
    /// <summary>
    /// User configurable settings in a <see cref="InGameMenu"/>
    /// </summary>
    public class Configuration: Obj {
        public readonly Property<LaneSpec> LaneSpecProp;
        public LaneSpec LaneSpec { get => LaneSpecProp.Value; set => LaneSpecProp.Value = value; }

        public readonly Property<RoadFinish> RoadFinishProp;
        public RoadFinish RoadFinish { get => RoadFinishProp.Value; set => RoadFinishProp.Value = value; }

        public readonly Property<ITool?> ToolProp;
        public ITool? Tool { get => ToolProp.Value; set => ToolProp.Value = value; }



        public Configuration() {
            LaneSpecProp = new Property<LaneSpec>(LaneSpec.Default, "laneSpec", this);
            RoadFinishProp = new Property<RoadFinish>(RoadFinish.Embankment, "roadFinish", this);
            ToolProp = new Property<ITool?>(null, "tool", this, Equality.ReferenceEqualComparer<ITool?>());
        }
    }
}
