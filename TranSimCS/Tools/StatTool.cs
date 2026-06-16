using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Tools {
    public class StatTool(InGameMenu menu) : ITool {
        public string Name => "Statistics";

        public string Description => "Counts everything. Junk included. \n"+menu.Stats.Format();

        public (object[], string)[] PromptKeys() => [];

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showStats);
        }
    }
}
