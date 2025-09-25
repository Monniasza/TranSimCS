using System;
using Eto.Forms;

internal static class ProgramHelpers {

    public static DialogResult SaveDialog(Action<SaveFileDialog> setup) {
    }

    public static void UsingFakeWindow(Action<Form> action) {
        var fakeWindow = new Form();
        fakeWindow.ShowInTaskbar = false;
        try {
            action(fakeWindow);
        } finally {
            fakeWindow.Close();
            fakeWindow.Dispose();
        }
    }
}