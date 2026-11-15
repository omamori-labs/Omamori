using System;

namespace Omamori.API
{
    /// <summary>
    ///     sounds and the music player, calls can come from any thread and are carried out on the update thread
    ///     true means the request was accepted, osu! can still refuse it when it runs
    ///     nothing happens, and false comes back, before the game has finished loading or when <see cref="Hook.Audio"/> isn't available
    /// </summary>
    public interface IModAudio
    {
        /// <summary>
        ///     plays a wav, mp3 or ogg from the mod's own folder, at a <paramref name="volume"/> from 0 to 1 under the player's effect volume
        ///     false when the file isn't there or the path leads outside the mod's folder
        /// </summary>
        bool PlaySound(string file, double volume = 1);

        /// <summary>
        ///     quietens the music until the result is disposed, the way osu! does behind a dialog
        ///     never during gameplay, where the music is the map, and a mod that's turned off has its ducking undone
        /// </summary>
        IDisposable DuckMusic();

        /// <summary>
        ///     the music controls below only work where osu!'s own do, so never during gameplay, false when they're refused
        /// </summary>
        bool PauseMusic();

        bool ResumeMusic();

        bool NextTrack();

        bool PreviousTrack();

        bool SeekMusic(TimeSpan position);
    }
}
