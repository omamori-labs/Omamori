using System;
using System.Collections;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Play;

namespace Omamori.Core.Plays
{
    /// <summary>
    ///     starting a play partway through, lazer itself only starts from the beginning or resumes from a pause
    /// </summary>
    internal static class LateStart
    {
        /// <summary>
        ///     drops the objects before <paramref name="startTime"/> so they aren't judged as misses
        ///     false with nothing changed when no object would be left
        /// </summary>
        public static bool Trim(IBeatmap beatmap, double startTime)
        {
            // a converted beatmap's objects are a List<T> underneath, sorted by start time
            if (beatmap.HitObjects is not IList { IsReadOnly: false, IsFixedSize: false } objects)
                return false;

            int before = 0;

            while (before < objects.Count && ((HitObject)objects[before]!).StartTime < startTime)
                before++;

            if (before == objects.Count)
                return false;

            for (int i = before - 1; i >= 0; i--)
                objects.RemoveAt(i);

            return true;
        }

        /// <summary>
        ///     stops the clock until the player resumes, osu!'s resume cursor goes on the first object for rulesets whose objects have positions
        /// </summary>
        public static void HoldForResume(Player player)
        {
            var ruleset = player.DrawableRuleset;
            var clock = player.GameplayClockContainer;

            clock.Stop();
            player.IsResuming = true;

            player.Schedule((Action)(() =>
            {
                // the resume overlay copies the gameplay cursor's position, and skips itself while that cursor is hidden
                // objects are placed in the playfield's own space, which the overlay doesn't share
                if (ruleset.Cursor is { } cursor && ruleset.ResumeOverlay != null && ruleset.Objects.FirstOrDefault() is IHasPosition first)
                {
                    cursor.LastFrameState = Visibility.Visible;
                    cursor.ActiveCursor.RelativePositionAxes = Axes.None;
                    cursor.ActiveCursor.Position = cursor.ActiveCursor.Parent!.ToLocalSpace(ruleset.Playfield.ToScreenSpace(first.Position));
                }

                ruleset.RequestResume(() =>
                {
                    clock.Start();
                    player.IsResuming = false;
                });
            }));
        }
    }
}
