using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace TranSimCS.SilkNet {
    /// <summary>
    /// Represents a simple message box. Particularly, its <see cref="ShowMessage"/> method is useful for showing it.
    /// </summary>
    public struct Message {
        public string Title;
        public string Text;
        public ImmutableArray<MessageAction> Actions;

        public Message(string title, string text, ImmutableArray<MessageAction> actions) {
            Text = text;
            Title = title;
            Actions = actions;
        }
        public Message(string title, string text, SilkNetTest window, params ImmutableArray<MessageAction> actions) {
            Text = text;
            Title = title;
            Actions = actions.Append(new MessageAction("OK", window.CloseModals)).ToImmutableArray();
        }


        public static Message ErrorMessage(string message, Exception exception, SilkNetTest window) => new Message(message, exception.ToString(), window);

        public void ShowMessage() {
            if (DearUI.Modal("message-box", Title)) {
                ImGui.TextWrapped(Text);
                foreach (var action in Actions) {
                    if (ImGui.Button(action.Text)) action.action();
                    ImGui.SameLine();
                }
                DearUI.EndModal();
            }
        }
    }

    public struct MessageAction {
        public string Text;
        public Action action;
        public MessageAction(string text, Action action) {
            Text = text;
            this.action = action;
        }
    }
}
