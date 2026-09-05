using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MLEM.Ui.Elements;
using Silk.NET.Input;

namespace TranSimCS.SilkNet {
    [Flags]
    public enum MouseState {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 4,
        X1 = 8,
        X2 = 16,
        X3 = 32,
        X4 = 64,
        X5 = 128,
        X6 = 256,
        X7 = 512,
        X8 = 1024,
        X9 = 2048,
    }
    public static class MouseStateMethods {
        public static MouseState ToMouseStateFlags(this MouseButton btn) {
            int idx = (int)btn;
            if (idx < 0) return MouseState.None;
            return MouseState.Left.ShiftLeft(idx);
        }
        public static bool IsMouseButtonDown(this MouseState state, MouseButton button) =>
            state.HasFlags(ToMouseStateFlags(button));
        public static MouseState SetButton(this MouseState state, MouseButton button, bool value) {
            var mask = button.ToMouseStateFlags();
            var result = state.WithFlags(mask, value);
            Debug.WriteLine(
                $"button={button}, idx={(int)button}, " +
                $"before={(int)state}, mask={(int)mask}, value={value}, after ={(int)result}");
            return result;
        }
    }
}
