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
        private readonly HookCheck hooks;

        public GameEvents(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public IDisposable OnScreenChanged(Action<ScreenChange> handler) => register(Hook.ScreenEvents, GameEvent.ScreenChanged, handler);

        public IDisposable OnBeatmapChanged(Action<BeatmapDetails> handler) => register(Hook.BeatmapEvents, GameEvent.BeatmapChanged, handler);

        public IDisposable OnRulesetChanged(Action<RulesetDetails> handler) => register(Hook.BeatmapEvents, GameEvent.RulesetChanged, handler);

        public IDisposable OnModsChanged(Action<IReadOnlyList<ModDetails>> handler) => register(Hook.BeatmapEvents, GameEvent.ModsChanged, handler);

        public IDisposable OnPlayStarted(Action<PlayDetails> handler) => register(Hook.PlayEvents, GameEvent.PlayStarted, handler);

        public IDisposable OnPlayEnded(Action<PlayResult> handler) => register(Hook.PlayEvents, GameEvent.PlayEnded, handler);

        public IDisposable OnPlayPauseChanged(Action<bool> handler) => register(Hook.PlayEvents, GameEvent.PlayPaused, handler);

        public IDisposable OnHit(Action<HitDetails> handler) => register(Hook.LivePlayEvents, GameEvent.Hit, handler);

        public IDisposable OnPlayStateChanged(Action<LivePlayState> handler) => register(Hook.LivePlayEvents, GameEvent.PlayStateChanged, handler);

        // every handler takes one argument internally
        public IDisposable OnGameExiting(Action handler) => register(Hook.LifecycleEvents, GameEvent.GameExiting, new Action<bool>(_ => handler()));

        public IDisposable OnWindowChanged(Action<WindowDetails> handler) => register(Hook.LifecycleEvents, GameEvent.WindowChanged, handler);

        public IDisposable OnTrackChanged(Action<BeatmapDetails> handler) => register(Hook.MusicEvents, GameEvent.TrackChanged, handler);

        public IDisposable OnMusicPlayingChanged(Action<bool> handler) => register(Hook.MusicEvents, GameEvent.MusicPlayingChanged, handler);

        public IDisposable OnUserChanged(Action<UserDetails> handler) => register(Hook.UserEvents, GameEvent.UserChanged, handler);

        public IDisposable OnStatisticsUpdated(Action<StatisticsUpdate> handler) => register(Hook.UserEvents, GameEvent.StatisticsUpdated, handler);

        public IDisposable OnSkinChanged(Action<SkinDetails> handler) => register(Hook.SkinEvents, GameEvent.SkinChanged, handler);

        public IDisposable OnScoreSaved(Action<ScoreDetails> handler) => register(Hook.LibraryEvents, GameEvent.ScoreSaved, handler);

        public IDisposable OnBeatmapSetAdded(Action<BeatmapSetDetails> handler) => register(Hook.LibraryEvents, GameEvent.BeatmapSetAdded, handler);

        public IDisposable OnBeatmapSetRemoved(Action<BeatmapSetDetails> handler) => register(Hook.LibraryEvents, GameEvent.BeatmapSetRemoved, handler);

        public IDisposable OnOverlayChanged(Action<OverlayChange> handler) => register(Hook.OverlayEvents, GameEvent.OverlayChanged, handler);

        public IDisposable OnRoomChanged(Action<RoomChange> handler) => register(Hook.MultiplayerEvents, GameEvent.RoomChanged, handler);

        private IDisposable register(Hook hook, GameEvent gameEvent, Delegate handler)
        {
            if (!hooks.CanRegister(hook, "handler"))
                return Subscription.NONE;

            string modId = manifest.Id;

            lock (handlers)
            {
                if (!handlers.TryGetValue(gameEvent, out var list))
                    handlers[gameEvent] = list = [];

                list.Add((modId, handler));
            }

            // by reference, the same handler registered twice is taken out once per dispose
            return new Subscription(() =>
            {
                lock (handlers)
                {
                    if (handlers.TryGetValue(gameEvent, out var list) && list.FindIndex(r => r.ModId == modId && ReferenceEquals(r.Handler, handler)) is var index and >= 0)
                        list.RemoveAt(index);
                }
            });
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
