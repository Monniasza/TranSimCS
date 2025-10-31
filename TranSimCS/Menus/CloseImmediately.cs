using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Menus.InGame;

namespace TranSimCS.Menus {
    public class CloseImmediately: Element {
        private readonly InGameMenu Menu;
        public CloseImmediately(InGameMenu menu): base(MLEM.Ui.Anchor.TopLeft, new(0, 0)) {
            Menu = menu;
            OnAddedToUi += CloseNow;
        }

        private void CloseNow(Element element) {
            TaskFactoryFactory<object?>.GetFactory().StartNew(() => {
                Menu.Overlay = null;
                return null;
            });
        }
    }
}
