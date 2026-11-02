namespace Omamori.API
{
    /// <summary>
    ///     the text on the player's discord status, set through osu!'s own rich presence 
    ///     the most recent mod to call <see cref="Set"/> controls presence
    /// </summary>
    public interface IModPresence
    {
        /// <summary>replaces whatever this mod set before, throws if an image is longer than discord allows</summary>
        void Set(PresenceStatus status);

        /// <summary>gives status back to the mod that set it before this one or to osu!</summary>
        void Clear();
    }
}
