using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using TranSimCS.Worlds.Stack;

namespace TranSimCS.Menus.InGame {
    public class ToolsPanel: Panel {
        private static Dictionary<string, Func<InGameMenu, Element>> panelCtors = new();
        public static event Action<string, Func<InGameMenu, Element>>? CtorAdded;

        public static void AddPanel(string attributeName, Func<InGameMenu, Element> ctor) {
            panelCtors.Add(attributeName, ctor);
            CtorAdded?.Invoke(attributeName, ctor);
        }

        //INSTANCE
        private readonly InGameMenu menu;
        private readonly Dictionary<string, Element> panels = new();
        public ToolsPanel(InGameMenu menu) : base(MLEM.Ui.Anchor.CenterLeft, new(200, 20.5f), true) {
            this.menu = menu;

            //Create panels
            panelCtors.ForEach(x => AddPanel0(x.Key, x.Value));
            CtorAdded += AddPanel0;

            //Listen to tool attribute changes
            menu.ToolAttributesProp.ValueChanged += ToolAttributesProp_ValueChanged;

            //Initialize
            Rebuild(menu.ToolAttributes);
        }
        private void AddPanel0(string attributeName, Func<InGameMenu, Element> ctor) {
            panels.Add(attributeName, ctor(menu));
        }

        private void ToolAttributesProp_ValueChanged(object? sender, Worlds.PropertyChangedEventArgs2<Iesi.Collections.Generic.ReadOnlySet<string>> e) {
            Rebuild(e.NewValue);
        }

        private void Rebuild(ISet<string> attribs) {
            //Delete all panels
            RemoveChildren();

            //Add tool panels back
            foreach (var attrib in attribs) {
                if(panels.TryGetValue(attrib, out var panel)){
                    AddChild(panel);
                }
            }
        }
        public T GetPanel<T>(string key) where T: Element => (T)panels[key];
    }
}
