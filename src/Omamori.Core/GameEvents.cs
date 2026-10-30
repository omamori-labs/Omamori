using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core
{
    internal enum GameEvent
    {
        ScreenChanged,
        BeatmapChanged,
        RulesetChanged,
        ModsChanged,
        PlayStarted,
        PlayEnded,
        PlayPaused,
        GameExiting,
        WindowChanged,
        TrackChanged,
        MusicPlayingChanged,
        UserChanged,
        StatisticsUpdated,
        SkinChanged,
        ScoreSaved,
        BeatmapSetAdded,
        BeatmapSetRemoved,
        OverlayChanged,
        RoomChanged,
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

        public void OnModsChanged(Action<IReadOnlyList<string>> handler) => register(Hook.BeatmapEvents, GameEvent.ModsChanged, handler);

        public void OnPlayStarted(Action<PlayDetails> handler) => register(Hook.PlayEvents, GameEvent.PlayStarted, handler);

        public void OnPlayEnded(Action<PlayResult> handler) => register(Hook.PlayEvents, GameEvent.PlayEnded, handler);

        public void OnPlayPaused(Action<bool> handler) => register(Hook.PlayEvents, GameEvent.PlayPaused, handler);

        // every handler takes one argument internally
        public void OnGameExiting(Action handler) => register(Hook.LifecycleEvents, GameEvent.GameExiting, new Action<bool>(_ => handler()));

        public void OnWindowChanged(Action<WindowDetails> handler) => register(Hook.LifecycleEvents, GameEvent.WindowChanged, handler);

        public void OnTrackChanged(Action<BeatmapDetails> handler) => register(Hook.MusicEvents, GameEvent.TrackChanged, handler);

        public void OnMusicPlayingChanged(Action<bool> handler) => register(Hook.MusicEvents, GameEvent.MusicPlayingChanged, handler);

        public void OnUserChanged(Action<UserDetails> handler) => register(Hook.UserEvents, GameEvent.UserChanged, handler);

        public void OnStatisticsUpdated(Action<StatisticsUpdate> handler) => register(Hook.UserEvents, GameEvent.StatisticsUpdated, handler);

        public void OnSkinChanged(Action<SkinDetails> handler) => register(Hook.SkinEvents, GameEvent.SkinChanged, handler);

        public void OnScoreSaved(Action<ScoreDetails> handler) => register(Hook.LibraryEvents, GameEvent.ScoreSaved, handler);

        public void OnBeatmapSetAdded(Action<BeatmapSetDetails> handler) => register(Hook.LibraryEvents, GameEvent.BeatmapSetAdded, handler);

        public void OnBeatmapSetRemoved(Action<BeatmapSetDetails> handler) => register(Hook.LibraryEvents, GameEvent.BeatmapSetRemoved, handler);

        public void OnOverlayChanged(Action<OverlayChange> handler) => register(Hook.OverlayEvents, GameEvent.OverlayChanged, handler);

        public void OnRoomChanged(Action<RoomChange> handler) => register(Hook.MultiplayerEvents, GameEvent.RoomChanged, handler);

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

        public static bool HasHandlers(GameEvent gameEvent)
        {
            lock (handlers)
                return handlers.TryGetValue(gameEvent, out var list) && list.Count > 0;
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
}
