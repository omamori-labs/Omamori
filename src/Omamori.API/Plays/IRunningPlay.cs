using System;

namespace Omamori.API
{
    /// <summary>
    ///     the play on screen right now, every call does nothing once the player has left it
    ///     calls can come from any thread, they're carried out on the update thread
    /// </summary>
    public interface IRunningPlay
    {
        /// <summary>how far into the map gameplay is</summary>
        TimeSpan Time { get; }

        bool IsPaused { get; }

        /// <summary>a solo play from song select, the only kind that can be seeked</summary>
        bool IsSolo { get; }

        /// <summary>shows the pause menu, osu! still refuses where it always would, like right after resuming or in multiplayer</summary>
        void Pause();

        /// <summary>resumes the way the pause menu's continue does</summary>
        void Resume();

        void Restart();

        /// <summary>leaves the play as if the player quit</summary>
        void Exit();

        /// <summary>
        ///     jumps to <paramref name="time"/>, objects skipped over count as misses
        ///     solo plays only, false otherwise, and the play is never submitted afterwards
        /// </summary>
        bool SeekTo(TimeSpan time);
    }
}
