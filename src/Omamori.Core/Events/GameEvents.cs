using System;
using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;

namespace Omamori.Core.Events
{
    internal sealed class GameEvents : IGameEvents
    {
        private static readonly Dictionary<GameEvent, List<(string ModId, Delegate Handler)>> handlers = [];

        private static readonly Dictionary<GameEvent, object> last_raised = [];

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
            if (!HookCheck.CanRegister(manifest, log, hinted, hook, "handler"))
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

            foreach (var (modId, handler) in list)
                ModFaults.RunForMod(modId, $"{gameEvent} handler", () => ((Action<T>)handler)(args));
        }

        /// <summary>
        ///     drops a value equal to the last one raised for this event, records compare by value
        /// </summary>
        public static void RaiseChanged<T>(GameEvent gameEvent, T value)
            where T : class
        {
            if (last_raised.TryGetValue(gameEvent, out object? previous) && EqualityComparer<T>.Default.Equals(previous as T, value))
                return;

            last_raised[gameEvent] = value;
            Raise(gameEvent, value);
        }

        internal static void Clear()
        {
            lock (handlers)
                handlers.Clear();

            last_raised.Clear();
        }
    }
}
