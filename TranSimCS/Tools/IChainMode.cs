using System;
using TranSimCS.Menus.InGame;
using TranSimCS.Roads;

namespace TranSimCS.Tools {
    public interface IChainMode: IEquatable<IChainMode?> {
        public string Name { get; }
        public LaneSpec ChainValues(InGameMenu game);
        bool IEquatable<IChainMode?>.Equals(IChainMode? other) {
            return ReferenceEquals(this, other);
        }
    }
    public class ChainModeChained : IChainMode {
        public static ChainModeChained value = new ChainModeChained();
        private ChainModeChained() { }
        public string Name => "From previous";
        public LaneSpec ChainValues(InGameMenu game) =>
            game.ConnectionTool.SourceNode?.Lane?.Spec
            ?? ChainModeCustom.value.ChainValues(game);
    }
    public class ChainModeCustom : IChainMode {
        public static ChainModeCustom value = new ChainModeCustom();
        private ChainModeCustom() { }
        public string Name => "Custom from road configurator";
        public LaneSpec ChainValues(InGameMenu game) => game.configurator.laneSpecProp.Value;
    }
}
