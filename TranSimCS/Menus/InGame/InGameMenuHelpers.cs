using MLEM.Ui;

namespace TranSimCS.Menus.InGame {
    internal static class InGameMenuHelpers {

        public static bool IsFocusedOnAny(this UiSystem system) {
            foreach (var root in system.GetRootElements()) {
                if (root.SelectedElement != null) return true;
            }
            return false;
        }
    }
}