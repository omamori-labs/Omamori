using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.Core.Details;
using Omamori.Core.Safety;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.Leaderboards;
using osu.Game.Screens.Select;

namespace Omamori.Core.ModUi
{
    internal enum MenuTarget
    {
        Beatmap,
        BeatmapSet,
        Score,
    }

    /// <summary>
    ///     mod items for lazer's beatmap and score right-click menus, built each time a menu opens
    /// </summary>
    internal static class ContextMenus
    {
        private static readonly List<(string ModId, MenuTarget Target, string Text, Action<object> Action)> registrations = [];

        public static void Add(string modId, MenuTarget target, string text, Action<object> action)
        {
            lock (registrations)
                registrations.Add((modId, target, text, action));
        }

        /// <summary>
        ///     the panel's own items with the mods' after them, untouched for a panel omamori doesn't know
        /// </summary>
        public static MenuItem[] Extend(object panel, MenuItem[]? items) =>
            detailsOf(panel) is var (target, details) ? Items(target, details, items ?? []) : items ?? [];

        /// <summary>
        ///     <paramref name="details"/> are taken when the menu opens, a mod that faulted since isn't offered
        /// </summary>
        public static MenuItem[] Items(MenuTarget target, object details, MenuItem[] items)
        {
            (string ModId, MenuTarget Target, string Text, Action<object> Action)[] mine;

            lock (registrations)
                mine = [.. registrations.Where(r => r.Target == target && !ModFaults.IsFaulted(r.ModId))];

            if (mine.Length == 0)
                return items;

            var added = mine.Select(r => (MenuItem)new OsuMenuItem(r.Text, MenuItemType.Standard, () => ModFaults.RunForMod(r.ModId, "menu item", () => r.Action(details))));

            return items.Length == 0 ? [.. added] : [.. items, new OsuMenuItemSpacer(), .. added];
        }

        internal static void Clear()
        {
            lock (registrations)
                registrations.Clear();
        }

        private static (MenuTarget Target, object Details)? detailsOf(object panel) => panel switch
        {
            PanelBeatmap p when p.beatmap is { } beatmap => (MenuTarget.Beatmap, GameDetails.Beatmap(beatmap)),
            PanelBeatmapStandalone p when p.beatmap is { } beatmap => (MenuTarget.Beatmap, GameDetails.Beatmap(beatmap)),
            PanelBeatmapSet p when p.groupedBeatmapSet is { } set => (MenuTarget.BeatmapSet, GameDetails.BeatmapSet(set.BeatmapSet)),
            BeatmapLeaderboardScore s => (MenuTarget.Score, GameDetails.Score(s.Score)),
            LeaderboardScore s => (MenuTarget.Score, GameDetails.Score(s.Score)),
            _ => null,
        };
    }
}
