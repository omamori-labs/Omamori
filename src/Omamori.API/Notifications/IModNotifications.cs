using System;

namespace Omamori.API
{
    /// <summary>
    ///     safe from any thread, the text is prefixed with the mod's name, posts from Load show once the game is up
    /// </summary>
    public interface IModNotifications
    {
        /// <summary>onClick runs on the update thread when the player clicks it</summary>
        void Post(string text, Action? onClick = null);

        /// <summary><see cref="Post"/> with an error icon</summary>
        void PostError(string text, Action? onClick = null);

        IModProgress PostProgress(string text);
    }
}
