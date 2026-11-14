using System;

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

        /// <summary>null to start from the beginning</summary>
        public TimeSpan? StartTime { get; private set; }

        /// <summary>the play never starts and the player stays on song select</summary>
        public void Cancel() => IsCancelled = true;

        /// <summary>
        ///     the play runs as normal but its score is never submitted, the same way lazer treats maps that aren't on the website
        ///     the first reason given is kept, retrying the play keeps it too
        ///     if omamori can't check a play for this, it isn't submitted while any mod is listening for plays starting
        /// </summary>
        public void SuppressSubmission(string reason) => SuppressionReason ??= reason;

        /// <summary>
        ///     starts the play at <paramref name="time"/> instead of the beginning, retrying the play starts there too
        ///     objects before it are left out, and the play waits on the resume overlay with osu!'s cursor on the first object left
        ///     a late start is never submitted, the first time given is kept, and a time past the last object starts from the beginning
        /// </summary>
        public void StartAt(TimeSpan time)
        {
            if (time <= TimeSpan.Zero || StartTime != null)
                return;

            StartTime = time;
            SuppressSubmission("started partway through the map");
        }
    }
}
