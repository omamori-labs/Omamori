namespace Omamori.API
{
    /// <summary>
    ///     a solo play about to start from song select, nothing has loaded yet
    /// </summary>
    public sealed class PlayStarting
    {
        public bool IsCancelled { get; private set; }

        /// <summary>null when nothing asked for this play to be left unsubmitted</summary>
        public string? SuppressionReason { get; private set; }

        /// <summary>the play never starts and the player stays on song select</summary>
        public void Cancel() => IsCancelled = true;

        /// <summary>
        ///     the play runs as normal but its score is never submitted, the same way lazer treats maps that aren't on the website
        ///     the first reason given is kept, retrying the play keeps it too
        /// </summary>
        public void SuppressSubmission(string reason) => SuppressionReason ??= reason;
    }
}
