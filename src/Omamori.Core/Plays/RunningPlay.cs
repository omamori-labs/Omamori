using System;
using Omamori.API;
using osu.Framework.Screens;
using osu.Game.Screens.Play;

namespace Omamori.Core.Plays
{
    /// <summary>
    ///     weak, so a mod holding on to it doesn't keep a finished player alive
    /// </summary>
    internal sealed class RunningPlay : IRunningPlay
    {
        private readonly WeakReference<Player> player;

        public RunningPlay(Player player)
        {
            this.player = new WeakReference<Player>(player);
        }

        public TimeSpan Time => player.TryGetTarget(out var p) && p.GameplayClockContainer is { } clock ? TimeSpan.FromMilliseconds(clock.CurrentTime) : TimeSpan.Zero;

        public bool IsPaused => player.TryGetTarget(out var p) && p.GameplayClockContainer is { } clock && clock.IsPaused.Value;

        public bool IsSolo => player.TryGetTarget(out var p) && p is SoloPlayer;

        public void Pause() => schedule(p => p.Pause());

        public void Resume() => schedule(p => p.Resume());

        public void Restart() => schedule(p => p.Restart(false));

        public void Exit() => schedule(p => p.PerformExit(false));

        public bool SeekTo(TimeSpan time)
        {
            if (!player.TryGetTarget(out var p) || p is not SoloPlayer)
                return false;

            // before the seek is even scheduled, so there's no window where a seeked play could still be submitted
            GamePlays.Unsubmit(p, "a mod moved it to another point in the map");
            schedule(q => q.Seek(time.TotalMilliseconds));
            return true;
        }

        private void schedule(Action<Player> action)
        {
            if (!player.TryGetTarget(out var p))
                return;

            p.Schedule((Action)(() =>
            {
                if (p.IsCurrentScreen())
                    action(p);
            }));
        }
    }
}
