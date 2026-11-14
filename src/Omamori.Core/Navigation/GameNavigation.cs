using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Overlays;
using osu.Framework.Screens;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Select;

namespace Omamori.Core.Navigation
{
    internal sealed class GameNavigation : IGameNavigation
    {
        // set once the game has loaded, everything that touches it after that is scheduled onto the update thread
        private static OsuGame? game;

        // a play asked for from the main menu, started once the song select pushed for it is entered
        private static Guid? pendingStart;

        private readonly HookCheck hooks;

        public GameNavigation(ModManifest manifest, IModLogger log)
        {
            hooks = new HookCheck(manifest, log);
        }

        public bool SelectBeatmap(int onlineId) =>
            withBeatmap(onlineId, (g, beatmap) => g.PresentBeatmap(beatmap.BeatmapSet!, d => d.ID == beatmap.ID));

        public bool PlayBeatmap(int onlineId) =>
            withBeatmap(onlineId, (g, beatmap) =>
                g.PerformFromScreen(screen => FeatureRegistry.Run(Feature.Navigation, () => play(g, screen, beatmap)), [typeof(SoloSongSelect)]));

        public bool ShowOverlay(GameOverlay overlay)
        {
            if (overlay == GameOverlay.Other || !usable(out var g))
                return false;

            if (GameOverlays.Of(g).FirstOrDefault(o => o.Kind == overlay).Overlay is not { } target)
                return false;

            schedule(g, target.Show);
            return true;
        }

        public void HideOverlays() => run(g => g.CloseAllOverlays(false));

        public void ShowBeatmapPage(int onlineId) => run(g => g.ShowBeatmap(onlineId));

        public void SearchBeatmaps(string query) => run(g => g.SearchBeatmapSet(query));

        public void ShowUser(int userId) => run(g => g.ShowUser(new APIUser { Id = userId }));

        public void ShowWiki(string path) => run(g => g.ShowWiki(path));

        public void OpenLink(string url) => run(g => g.HandleLink(url));

        public static void Attach(OsuGame loaded) => game = loaded;

        /// <summary>
        ///     the song select pushed from the main menu for <see cref="PlayBeatmap"/> starts the play once it's on screen
        /// </summary>
        public static void SongSelectEntered(SongSelect songSelect)
        {
            if (pendingStart is not { } id || songSelect is not SoloSongSelect solo)
                return;

            pendingStart = null;

            if (solo.Beatmap.Value.BeatmapInfo.ID == id)
                solo.Schedule((Action)(() => solo.OnStart()));
        }

        internal static void Clear()
        {
            game = null;
            pendingStart = null;
        }

        private static void play(OsuGame g, IScreen screen, BeatmapInfo beatmap)
        {
            select(g, beatmap);

            if (screen is SoloSongSelect songSelect)
            {
                songSelect.OnStart();
                return;
            }

            // the runner falls back to the main menu when there's no song select to return to
            pendingStart = beatmap.ID;
            screen.Push(new SoloSongSelect());
        }

        // song select starts whatever is selected, so the ruleset and beatmap are set before it's asked to
        private static void select(OsuGame g, BeatmapInfo beatmap)
        {
            // an osu! map converts to every ruleset, any other only plays in its own
            if (beatmap.Ruleset.OnlineID != 0 && g.RulesetStore.GetRuleset(beatmap.Ruleset.ShortName) is { } ruleset)
                g.Ruleset.Value = ruleset;

            g.Beatmap.Value = g.BeatmapManager.GetWorkingBeatmap(beatmap);
        }

        private bool withBeatmap(int onlineId, Action<OsuGame, BeatmapInfo> action)
        {
            if (!usable(out var g))
                return false;

            // detached, so it's safe to look up off the update thread and keep until the scheduled action runs
            var beatmap = FeatureRegistry.Run<BeatmapInfo?>(Feature.Navigation, null,
                _ => g.BeatmapManager.QueryBeatmap("OnlineID == $0 AND BeatmapSet.DeletePending == false", onlineId));

            if (beatmap?.BeatmapSet == null)
                return false;

            schedule(g, () => action(g, beatmap));
            return true;
        }

        private void run(Action<OsuGame> action)
        {
            if (usable(out var g))
                schedule(g, () => action(g));
        }

        private bool usable([NotNullWhen(true)] out OsuGame? current)
        {
            current = game;
            return current != null && hooks.CanRegister(Hook.Navigation, "navigation");
        }

        private static void schedule(OsuGame g, Action action) => g.Schedule((Action)(() => FeatureRegistry.Run(Feature.Navigation, action)));
    }
}
