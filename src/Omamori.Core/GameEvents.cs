using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;
using osu.Framework.Screens;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.Play;

namespace Omamori.Core
{
    internal enum GameEvent
    {
        ScreenChanged,
        BeatmapChanged,
        RulesetChanged,
        PlayStarted,
        PlayEnded,
    }

    internal sealed class GameEvents : IGameEvents
    {
        private static readonly Dictionary<GameEvent, List<(string ModId, Delegate Handler)>> handlers = [];

        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public GameEvents(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void OnScreenChanged(Action<ScreenChange> handler) => register(Hook.ScreenEvents, GameEvent.ScreenChanged, handler);

        public void OnBeatmapChanged(Action<BeatmapDetails> handler) => register(Hook.BeatmapEvents, GameEvent.BeatmapChanged, handler);

        public void OnRulesetChanged(Action<RulesetDetails> handler) => register(Hook.BeatmapEvents, GameEvent.RulesetChanged, handler);

        public void OnPlayStarted(Action<PlayDetails> handler) => register(Hook.PlayEvents, GameEvent.PlayStarted, handler);

        public void OnPlayEnded(Action<PlayResult> handler) => register(Hook.PlayEvents, GameEvent.PlayEnded, handler);

        private void register(Hook hook, GameEvent gameEvent, Delegate handler)
        {
            if (!HookUse.CanRegister(manifest, log, hinted, hook, "handler"))
                return;

            lock (handlers)
            {
                if (!handlers.TryGetValue(gameEvent, out var list))
                    handlers[gameEvent] = list = [];

                list.Add((manifest.Id, handler));
            }
        }

        /// <summary>
        ///     update thread only, a handler that throws turns its mod off and the rest still run
        /// </summary>
        public static void Raise<T>(GameEvent gameEvent, T args)
        {
            List<(string ModId, Delegate Handler)> list;

            lock (handlers)
            {
                if (!handlers.TryGetValue(gameEvent, out var registered))
                    return;

                list = [.. registered];
            }

            foreach (var (modId, handler) in list.Where(h => !ModFaults.IsFaulted(h.ModId)))
            {
                try
                {
                    ((Action<T>)handler)(args);
                }
                catch (Exception e)
                {
                    if (ModLoader.Get(modId) is { } mod)
                        ModFaults.Contain(mod, e, scheduler: null);
                    else
                        LoaderLog.Error($"[{modId}] {gameEvent} handler threw.", e);
                }
            }
        }

        internal static void Clear()
        {
            lock (handlers)
                handlers.Clear();
        }
    }

    internal static class HookUse
    {
        /// <summary>
        ///     an undeclared hook counts as optional and is hinted once, an unavailable one is skipped
        /// </summary>
        public static bool CanRegister(ModManifest manifest, IModLogger log, HashSet<Hook> hinted, Hook hook, string what)
        {
            if (!manifest.Hooks.ContainsKey(hook) && hinted.Add(hook))
                log.Info($"uses the {hook} hook without declaring it in {ModLoader.MANIFEST_FILENAME}, treating it as optional.");

            if (FeatureRegistry.IsAvailable(hook))
                return true;

            log.Info($"{hook} hook isn't available on this osu! version, skipping this {what}.");
            return false;
        }
    }

    /// <summary>
    ///     turns lazer's objects into the copies mods get, only public lazer state is read
    /// </summary>
    internal static class GameEventSources
    {
        private static PlayDetails? activePlay;
        private static BeatmapDetails? lastBeatmap;

        public static void WatchScreens(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (last, next) => screenChanged(last, next);
            game.ScreenStack.ScreenExited += (last, next) => screenChanged(last, next);
        }

        public static void WatchBeatmap(OsuGame game)
        {
            game.Beatmap.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                if (e.NewValue is DummyWorkingBeatmap)
                    return;

                // song select swaps in a new working beatmap for the same map, records compare by value
                var beatmap = Beatmap(e.NewValue.BeatmapInfo);
                if (beatmap == lastBeatmap)
                    return;

                lastBeatmap = beatmap;
                GameEvents.Raise(GameEvent.BeatmapChanged, beatmap);
            }), true);

            game.Ruleset.BindValueChanged(e => FeatureRegistry.Run(Feature.BeatmapEvents, () =>
            {
                // pattern matching, == on realm objects needs realm referenced
                if (e.NewValue is { } ruleset)
                    GameEvents.Raise(GameEvent.RulesetChanged, new RulesetDetails { ShortName = ruleset.ShortName, Name = ruleset.Name });
            }), true);
        }

        public static void WatchPlays(OsuGame game)
        {
            game.ScreenStack.ScreenPushed += (last, next) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player && next is osu.Game.Screens.Ranking.ResultsScreen results)
                    endPlay(results.Score);

                if (next is Player player)
                    startPlay(player);
            });

            // results exiting resumes the player, which isn't valid for resume and exits too, activePlay keeps that from ending twice
            game.ScreenStack.ScreenExited += (last, _) => FeatureRegistry.Run(Feature.PlayEvents, () =>
            {
                if (last is Player)
                    endPlay(null);
            });
        }

        private static void screenChanged(IScreen? last, IScreen? next) => FeatureRegistry.Run(Feature.ScreenEvents, () =>
            GameEvents.Raise(GameEvent.ScreenChanged, new ScreenChange
            {
                Screen = Kind(next),
                Previous = Kind(last),
                ScreenName = next?.GetType().Name ?? string.Empty,
            }));

        private static void startPlay(Player player)
        {
            if (activePlay != null)
                endPlay(null);

            activePlay = new PlayDetails
            {
                Beatmap = Beatmap(player.Beatmap.Value.BeatmapInfo),
                Ruleset = player.Ruleset.Value is { } ruleset ? ruleset.ShortName : string.Empty,
                Mods = acronyms(player.Mods.Value),
                IsReplay = player is ReplayPlayer or SpectatorPlayer,
            };

            GameEvents.Raise(GameEvent.PlayStarted, activePlay);
        }

        private static void endPlay(ScoreInfo? score)
        {
            if (activePlay is not { } play)
                return;

            activePlay = null;
            GameEvents.Raise(GameEvent.PlayEnded, new PlayResult { Play = play, Completed = score is not null, Score = score is null ? null : Score(score) });
        }

        internal static GameScreen Kind(IScreen? screen) => screen switch
        {
            MainMenu => GameScreen.MainMenu,
            osu.Game.Screens.Select.SongSelect => GameScreen.SongSelect,
            Player => GameScreen.Gameplay,
            osu.Game.Screens.Ranking.ResultsScreen => GameScreen.Results,
            osu.Game.Screens.Edit.Editor => GameScreen.Editor,
            osu.Game.Screens.OnlinePlay.Multiplayer.Multiplayer => GameScreen.Multiplayer,
            osu.Game.Screens.OnlinePlay.Playlists.Playlists => GameScreen.Playlists,
            _ => GameScreen.Other,
        };

        internal static BeatmapDetails Beatmap(BeatmapInfo beatmap) => new BeatmapDetails
        {
            Title = beatmap.Metadata.Title,
            Artist = beatmap.Metadata.Artist,
            Creator = beatmap.Metadata.Author is { } author ? author.Username : string.Empty,
            Difficulty = beatmap.DifficultyName,
            StarRating = beatmap.StarRating,
            Bpm = beatmap.BPM,
            Length = TimeSpan.FromMilliseconds(beatmap.Length),
            OnlineId = beatmap.OnlineID > 0 ? beatmap.OnlineID : null,
            Ruleset = beatmap.Ruleset is { } ruleset ? ruleset.ShortName : string.Empty,
        };

        internal static ScoreDetails Score(ScoreInfo score) => new ScoreDetails
        {
            TotalScore = score.TotalScore,
            Accuracy = score.Accuracy,
            Rank = score.Rank.ToString(),
            MaxCombo = score.MaxCombo,
            Pp = score.PP,
            Passed = score.Passed,
            Date = score.Date,
        };

        private static IReadOnlyList<string> acronyms(IReadOnlyList<Mod>? mods) => [.. (mods ?? []).Select(m => m.Acronym)];
    }
}
