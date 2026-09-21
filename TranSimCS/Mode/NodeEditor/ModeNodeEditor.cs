using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.SilkNet;

namespace TranSimCS.Mode.NodeEditor {
    public sealed class ModeNodeEditor: IMode {
        public SilkNetTest Game { get; private set; }
        public ModeNodeEditor(SilkNetTest game) {
            Game = game;
        }

        public string Title() => "Road Node Editor";
    }
}
