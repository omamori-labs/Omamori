namespace Omamori.API
{
    /// <summary>
    ///     what kind of play it is, only <see cref="Solo"/> plays can be left unsubmitted
    ///     <see cref="Other"/> comes first so a play nobody filled in never passes for a solo one
    /// </summary>
    public enum PlayKind
    {
        Other,
        Solo,
        Playlist,
        Multiplayer,
        Replay,
        Spectator,
    }
}
