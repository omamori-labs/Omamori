using System;

namespace Omamori.API
{
    /// <summary>
    ///     the play on screen right now, calls can come from any thread and are carried out on the update thread
    ///     true means the request was accepted, osu! can still refuse it when it runs, false means the player has left the play
    /// </summary>
    public interface IRunningPlay
    {
        /// <summary>how far into the map gameplay is</summary>
        TimeSpan Time { get; }

        bool IsPaused { get; }

        /// <summary><see cref="PlayKind.Other"/> once the player has left the play</summary>
        PlayKind Kind { get; }

        /// <summary>a solo play from song select, the only kind that can be seeked</summary>
        bool IsSolo => Kind == PlayKind.Solo;

        /// <summary>shows the pause menu, osu! still refuses where it always would, like right after resuming or in multiplayer</summary>
        bool Pause();

        /// <summary>resumes the way the pause menu's continue does</summary>
        bool Resume();

        bool Restart();

        /// <summary>leaves the play as if the player quit</summary>
        bool Exit();

        /// <summary>
        ///     jumps to <paramref name="time"/>, objects skipped over count as misses
        ///     solo plays only, false otherwise, and the play is never submitted afterwards
        /// </summary>
        bool SeekTo(TimeSpan time);
    }
}
