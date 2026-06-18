using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Roads;
using TranSimCS.Roads.Node;
using TranSimCS.Roads.Strip;

namespace TranSimCS.Menus.InGame {
    public static class SelectionMethods {
        public static IRoadElement? AsRoadElement(this Selection selection) => selection.As<IRoadElement>();
        public static Lane? GetLane(this Selection selection) => (selection.Tag as IRoadElement)?.GetLane();
        public static LaneStrip? GetLaneStrip(this Selection selection) => (selection.Tag as IRoadElement)?.GetLaneStrip();
        public static RoadStrip? GetRoadStrip(this Selection selection) => selection.AsRoadElement()?.GetRoadStrip();
        public static RoadNodeEnd GetRoadNodeEnd(this Selection selection) => selection.AsRoadElement()?.GetNodeEnd();

        public static T? As<T>(this Selection selection) {
            if (selection.Tag is T attempt1) return attempt1;
            if (selection.SelectedObj is T attempt2) return attempt2;
            return default;
        }
    }
}
