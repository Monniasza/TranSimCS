namespace TranSimCS.Mode {
    /// <summary>
    /// Cursor types for the unified cursor system.
    /// </summary>
    public enum CursorType {
        /// <summary>Default OS cursor, no mode-specific action.</summary>
        Default,
        /// <summary>The next click will add an element. Symbol: +</summary>
        Add,
        /// <summary>The next click will remove an element. Symbol: -</summary>
        Remove,
        /// <summary>The action is unavailable. Symbol: X</summary>
        Unavailable,
        /// <summary>The next click will open or interact with an element. Symbol: O</summary>
        Open
    }
}
