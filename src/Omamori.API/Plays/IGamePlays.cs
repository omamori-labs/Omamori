using System;

namespace Omamori.API
{
    /// <summary>
    ///     control over plays, <see cref="IGameEvents"/> only watches them
    ///     disposing what registering returns stops that handler, there's no need to when the mod is turned off
    /// </summary>
    public interface IGamePlays
    {
        /// <summary>
        ///     runs on the update thread when the player starts a solo play from song select, in mod load order
        ///     rooms don't raise it, picking a map there doesn't start a play
        /// </summary>
        IDisposable OnStarting(Action<PlayStarting> handler);

        /// <summary>
        ///     the play on screen, null outside gameplay, behind results, or when <see cref="Hook.PlayControl"/> isn't available
        /// </summary>
        IRunningPlay? Running { get; }

        /// <summary>
        ///     runs on the update thread when a solo play's health runs out, in mod load order
        ///     once a mod prevents it, it isn't raised again for the rest of that play
        /// </summary>
        IDisposable OnFailing(Action<PlayFailing> handler);
    }
}
