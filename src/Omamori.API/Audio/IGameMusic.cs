using System;

namespace Omamori.API
{
    /// <summary>
    ///     osu!'s music player, the one the now playing overlay shows, safe to read and call from any thread
    ///     true means the request was accepted, osu! can still refuse it when it runs
    ///     the controls only work where osu!'s own do, so never during gameplay, where the music is the map
    ///     nothing happens, and false comes back, before the game has finished loading or when <see cref="Hook.Audio"/> isn't available
    /// </summary>
    public interface IGameMusic
    {
        /// <summary>the song the music player is on, null before the game has finished loading</summary>
        BeatmapDetails? Current { get; }

        bool IsPlaying { get; }

        TimeSpan Position { get; }

        /// <summary>zero while the track is still loading</summary>
        TimeSpan Length { get; }

        bool Pause();

        bool Resume();

        bool NextTrack();

        bool PreviousTrack();

        bool SeekTo(TimeSpan position);

        /// <summary>
        ///     quietens the music until the result is disposed, the way osu! does behind a dialog
        ///     never during gameplay, and a mod that's turned off has its ducking undone
        /// </summary>
        IDisposable Duck();
    }
}
