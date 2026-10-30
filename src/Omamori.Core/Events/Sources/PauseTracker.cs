namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     the gameplay clock starts paused and stops when the play ends, only the player's own pauses in between count
    /// </summary>
    internal sealed class PauseTracker
    {
        private bool started;

        public bool IsPlayerPause(bool paused, bool completed)
        {
            if (completed)
                return false;

            if (!started)
            {
                started = !paused;
                return false;
            }

            return true;
        }
    }
}
