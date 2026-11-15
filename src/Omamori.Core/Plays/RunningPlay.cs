using System;
using Omamori.API;
using Omamori.Core.Details;
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

        public PlayKind Kind => player.TryGetTarget(out var p) && GamePlays.IsOnScreen(p) ? GameDetails.KindOf(p.GetType()) : PlayKind.Other;

        public bool Pause() => schedule(p => p.Pause());

        public bool Resume() => schedule(p => p.Resume());

        public bool Restart() => schedule(p => p.Restart(false));

        public bool Exit() => schedule(p => p.PerformExit(false));

        public bool SeekTo(TimeSpan time)
        {
            if (!player.TryGetTarget(out var p) || p is not SoloPlayer || !GamePlays.IsOnScreen(p))
                return false;

            // before the seek is even scheduled, so there's no window where a seeked play could still be submitted
            GamePlays.Unsubmit(p, "a mod moved it to another point in the map");
            schedule(q => q.Seek(time.TotalMilliseconds));
            return true;
        }

        /// <summary>false once the player has left the play, it's checked again on the update thread</summary>
        private bool schedule(Action<Player> action)
        {
            if (!player.TryGetTarget(out var p) || !GamePlays.IsOnScreen(p))
                return false;

            p.Schedule((Action)(() =>
            {
                if (p.IsCurrentScreen())
                    action(p);
            }));

            return true;
        }
    }
}
