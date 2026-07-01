using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Roads.Node {
    public enum LaneDirectionResult {
        UndefinedOnly, UndefinedReversible, BackwardOnly, BackReversible, ForwardOnly, ForwardReversible
    }
    public static class LaneMethods {
        public static bool IsLanePassable(this Lane lane) {
            bool fromBack = false, toBack = false, fromFront = false, toFront = false;
            foreach (var strip in lane.Connections) {
                fromFront |= strip.StartLane == lane.Front;
                toFront |= strip.EndLane == lane.Front;
                fromBack |= strip.StartLane == lane.Rear;
                toBack |= strip.EndLane == lane.Rear;
            }
            return (fromBack && toFront) || (fromFront && toBack);
        }
        public static (int Forward, int Backward) CountLaneDirections(this Lane lane) {
            int forward = 0;
            int backward = 0;
            foreach (var strip in lane.Connections) {
                if (strip.StartLane == lane.Front) forward++;
                if (strip.EndLane == lane.Front) backward++;
                if (strip.StartLane == lane.Rear) backward++;
                if (strip.EndLane == lane.Rear) forward++;
            }
            return (forward, backward);
        }
        public static LaneDirectionResult Classify(int forward, int backward) {
            if (forward == 0 && backward == 0) return LaneDirectionResult.UndefinedOnly;
            if (forward == backward) return LaneDirectionResult.UndefinedReversible;
            if (backward == 0) return LaneDirectionResult.ForwardOnly;
            if (forward == 0) return LaneDirectionResult.BackwardOnly;
            if (backward > forward) return LaneDirectionResult.BackReversible;
            return LaneDirectionResult.ForwardReversible;
        }
        public static LaneDirectionResult FindLaneDirection(this Lane lane) {
            var (forward, backward) = CountLaneDirections(lane);
            return Classify(forward, backward);
        }
        public static bool IsReversible(this LaneDirectionResult result) =>
            result is LaneDirectionResult.ForwardReversible or LaneDirectionResult.BackReversible or LaneDirectionResult.UndefinedReversible;

        public static bool IsForward(this LaneDirectionResult result) =>
            result is LaneDirectionResult.ForwardOnly or LaneDirectionResult.ForwardReversible;

        public static bool IsBackward(this LaneDirectionResult result) =>
            result is LaneDirectionResult.BackwardOnly or LaneDirectionResult.BackReversible;

        public static bool IsIndeterminate(this LaneDirectionResult result) =>
            result is LaneDirectionResult.UndefinedOnly or LaneDirectionResult.UndefinedReversible;

        public static bool AllowsForward(this LaneDirectionResult result) =>
            result is LaneDirectionResult.ForwardOnly or LaneDirectionResult.UndefinedReversible or LaneDirectionResult.ForwardReversible or LaneDirectionResult.BackReversible;
        public static bool AllowsReverse(this LaneDirectionResult result) =>
            result is LaneDirectionResult.BackwardOnly or LaneDirectionResult.UndefinedReversible or LaneDirectionResult.BackReversible or LaneDirectionResult.ForwardReversible;
    }
}
