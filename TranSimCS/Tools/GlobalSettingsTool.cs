using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TranSimCS.Tools {
    public class GlobalSettingsTool : ITool {
        public string Name => "Game Settings";

        public string Description => "Configure the game to your liking";

        public (object[], string)[] PromptKeys() => [];

        void ITool.AddAttributes(ISet<string> action) {
            action.Add(ToolAttribs.showSettings);
        }
    }
}
