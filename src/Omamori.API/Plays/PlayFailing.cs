namespace Omamori.API
{
    /// <summary>
    ///     a solo play whose health just ran out
    /// </summary>
    public sealed class PlayFailing
    {
        public bool IsPrevented { get; private set; }

        /// <summary>the player keeps playing on empty health, and the play is never submitted</summary>
        public void Prevent() => IsPrevented = true;
    }
}
