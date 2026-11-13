using System;

namespace Omamori.API
{
    /// <summary>
    ///     control over plays before they begin, <see cref="IGameEvents"/> only watches them
    /// </summary>
    public interface IGamePlays
    {
        /// <summary>
        ///     runs on the update thread when the player starts a solo play from song select, in mod load order
        ///     rooms don't raise it, picking a map there doesn't start a play
        /// </summary>
        void OnStarting(Action<PlayStarting> handler);
    }
}
