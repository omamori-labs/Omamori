namespace Omamori.API
{
    /// <summary>
    ///     the mod's own sounds, osu!'s music player is <see cref="IModContext.GameMusic"/>
    ///     calls can come from any thread and are carried out on the update thread
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
    }
}
