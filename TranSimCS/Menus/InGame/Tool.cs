using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using MLEM.Input;

namespace TranSimCS.Menus.InGame {
    public interface ITool {
        public string Name { get; }
        public string Description { get; }
        public void OnClick(MouseButton button);
        public void OnRelease(MouseButton button);
        public void OnKeyDown(Keys key);
        public void OnKeyUp(Keys key);
    }

    public class RoadDemolitionTool(Game1 game) : ITool {
        string ITool.Name => "Road Demolition Tool";

        string ITool.Description => "LMB to demolish the selected road segment, RMB to demolish only the selected lane";

        void ITool.OnClick(MouseButton button) {
            throw new NotImplementedException();
        }

        void ITool.OnKeyDown(Keys key) {
            throw new NotImplementedException();
        }

        void ITool.OnKeyUp(Keys key) {
            throw new NotImplementedException();
        }

        void ITool.OnRelease(MouseButton button) {
            throw new NotImplementedException();
        }
    }
}
