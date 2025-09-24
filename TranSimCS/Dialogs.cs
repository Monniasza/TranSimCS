using System;
using Eto.Forms;

internal static class Dialogs {
    public static DialogResult ShowWithFakeWindow(FileDialog dialog) {
        DialogResult result = DialogResult.None;
        Dialogs.UsingFakeWindow((form) => {
            result = dialog.ShowDialog(form);
        });
        return result;
    }
    public static (SaveFileDialog, DialogResult) SaveDialog(Action<SaveFileDialog>? postsave, string? path = null) {
        SaveFileDialog dialog = new SaveFileDialog();
        var folder = path ?? Program.SaveDirectory;
        dialog.Directory = new Uri(folder, UriKind.RelativeOrAbsolute);
        DialogResult result = ShowWithFakeWindow(dialog);
        return (dialog, result);
    }
    public static (OpenFileDialog, DialogResult) LoadDialog(Action<OpenFileDialog>? postload, string? path = null) {
        OpenFileDialog dialog = new OpenFileDialog();
        var folder = path ?? Program.SaveDirectory;
        dialog.Directory = new Uri(folder, UriKind.RelativeOrAbsolute);
        DialogResult result = ShowWithFakeWindow(dialog);
        return (dialog, result);
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