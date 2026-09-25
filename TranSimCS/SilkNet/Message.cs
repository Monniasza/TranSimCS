using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
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
        public string? Details;
        public ImmutableArray<MessageAction> Actions;

        public Message(string title, string text, ImmutableArray<MessageAction> actions) {
            Text = text;
            Title = title;
            Details = null;
            Actions = actions;
        }
        public Message(string title, string text, GameWindow window, params ImmutableArray<MessageAction> actions) : this(title, text, actions) {
            Actions = actions.Append(new MessageAction("OK", window.CloseModals)).ToImmutableArray();
        }
        public Message(string title, string text, string? details, GameWindow window, params ImmutableArray<MessageAction> actions) : this(title, text, actions) {
            Details = details;
            Actions = actions.Append(new MessageAction("OK", window.CloseModals)).ToImmutableArray();
        }


        public static Message ErrorMessage(string message, Exception exception, GameWindow window)
            => new Message(message, exception.Message, exception.ToString(), window);

        public void ShowMessage() {
            if (DearUI.Modal("message-box", Title)) {
                ImGui.TextWrapped(Text);
                if(!string.IsNullOrEmpty(Details) && ImGui.CollapsingHeader("Details")) {
                    var detailsSize = new Vector2(0, ImGui.GetTextLineHeightWithSpacing() * 15);
                    ImGui.BeginChild("details", detailsSize, ImGuiChildFlags.None);
                    ImGui.TextWrapped(Details);
                    ImGui.EndChild();
                }
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
