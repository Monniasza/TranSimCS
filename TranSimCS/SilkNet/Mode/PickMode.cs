using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Input;
using TranSimCS.Model;
using TranSimCS.Worlds;

namespace TranSimCS.SilkNet.Mode {
    public class PickMode: IMode {
        public SilkNetTest Window{ get; private set; }
        internal PickMode(SilkNetTest window) {
            Window = window;
        }

        public string Title() => "Select";

        void IMode.OnClose() {
            Window.Sticky = null;
        }
        void IMode.OnMousePress(MouseButton button) {
            switch (button) {
                case MouseButton.Left:
                    //Select the object
                    Window.Sticky = Window.MouseOver;
                    break;
                case MouseButton.Right:
                    Window.Sticky = null;
                    break;
            }
        }
    }
}
