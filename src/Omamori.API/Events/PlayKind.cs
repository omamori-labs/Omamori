namespace Omamori.API
{
    /// <summary>
    ///     what kind of play it is, only <see cref="Solo"/> plays can be left unsubmitted
    /// </summary>
    public enum PlayKind
    {
        Solo,
        Playlist,
        Multiplayer,
        Replay,
        Spectator,
        Other,
    }
}
