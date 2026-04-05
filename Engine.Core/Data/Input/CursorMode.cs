namespace PBG.Data
{
    public enum CursorMode
    {
        /// <summary>
        /// Cursor is visible and has no restrictions on mobility.
        /// </summary>
        Normal,

        /// <summary>
        /// Cursor is invisible, and has no restrictions on mobility.
        /// </summary>
        Hidden,

        /// <summary>
        /// Cursor is invisible, and is restricted to the center of the screen.
        /// </summary>
        /// <remarks>
        /// Only supported by GLFW, throws on SDL if used.
        /// </remarks>
        Disabled,

        /// <summary>
        /// Cursor is invisible, and is restricted to the center of the screen. Mouse motion is not scaled.
        /// </summary>
        Raw
    }
}