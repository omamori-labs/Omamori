using Omamori.API;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     which play is running, so each one ends exactly once
    /// </summary>
    internal sealed class PlayTracker
    {
        private PlayDetails? active;

        /// <summary>the play that was still running, it has to end before this one starts</summary>
        public PlayDetails? Start(PlayDetails play)
        {
            var interrupted = active;
            active = play;
            return interrupted;
        }

        /// <summary>null when nothing is running, like the player exiting again after its results</summary>
        public PlayDetails? End()
        {
            var ended = active;
            active = null;
            return ended;
        }
    }
}
