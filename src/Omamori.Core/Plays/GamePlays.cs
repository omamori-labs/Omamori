using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Game.Beatmaps;
using osu.Game.Screens.Play;

namespace Omamori.Core.Plays
{
    internal sealed class GamePlays : IGamePlays
    {
        private const string broken_check = "omamori couldn't check whether a mod asked to leave it unsubmitted";

        private static readonly List<(string ModId, Action<PlayStarting> Handler)> handlers = [];

        // keyed by the player, so a retry that makes a new one is still covered by the decision made on song select
#pragma warning disable IDE0028 // a weak table has no collection expression form
        private static readonly ConditionalWeakTable<object, PlayStarting> started = new ConditionalWeakTable<object, PlayStarting>();

        // only players whose beatmap was actually cut down, a start past the last object plays from the beginning
        private static readonly ConditionalWeakTable<object, StrongBox<double>> late_starts = new ConditionalWeakTable<object, StrongBox<double>>();

        // why a play that already had its token was left unsubmitted, first reason kept
        private static readonly ConditionalWeakTable<object, string> unsubmitted_mid_play = new ConditionalWeakTable<object, string>();

        // a fail a mod prevented stays prevented, health stays empty so lazer asks again on every judgement
        private static readonly ConditionalWeakTable<object, PlayFailing> prevented_fails = new ConditionalWeakTable<object, PlayFailing>();
#pragma warning restore IDE0028

        private static readonly List<(string ModId, Action<PlayFailing> Handler)> failing_handlers = [];

        // update thread only
        private static WeakReference<Player>? onScreen;

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public GamePlays(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        /// <summary>what song select decided for the play it is starting right now, null outside of that</summary>
        public static PlayStarting? Current { get; private set; }

        public IDisposable OnStarting(Action<PlayStarting> handler)
        {
            if (!hooks.CanRegister(Hook.PlayStarting, "handler"))
                return Subscription.NONE;

            return add(handlers, handler);
        }

        public IRunningPlay? Running =>
            FeatureRegistry.IsAvailable(Hook.PlayControl) && onScreen is { } current && current.TryGetTarget(out var player) ? new RunningPlay(player) : null;

        public IDisposable OnFailing(Action<PlayFailing> handler)
        {
            if (!hooks.CanRegister(Hook.PlayControl, "fail handler"))
                return Subscription.NONE;

            return add(failing_handlers, handler);
        }

        private Subscription add<T>(List<(string ModId, Action<T> Handler)> list, Action<T> handler)
        {
            string modId = manifest.Id;

            lock (list)
                list.Add((modId, handler));

            return new Subscription(() =>
            {
                lock (list)
                {
                    if (list.FindIndex(r => r.ModId == modId && ReferenceEquals(r.Handler, handler)) is var index and >= 0)
                        list.RemoveAt(index);
                }
            });
        }

        public static void Entered(Player player) => onScreen = new WeakReference<Player>(player);

        /// <summary>the field is only written on the update thread, a stale read from another one is checked again when the request runs</summary>
        public static bool IsOnScreen(Player player) => onScreen is { } current && current.TryGetTarget(out var shown) && ReferenceEquals(shown, player);

        public static void Left(Player player)
        {
            if (onScreen is { } current && current.TryGetTarget(out var shown) && ReferenceEquals(shown, player))
                onScreen = null;
        }

        /// <summary>false when a mod prevented the fail, which also leaves the play unsubmitted</summary>
        public static bool AllowFail(object player)
        {
            if (prevented_fails.TryGetValue(player, out _))
                return false;

            (string ModId, Action<PlayFailing> Handler)[] snapshot;

            lock (failing_handlers)
                snapshot = [.. failing_handlers];

            var failing = new PlayFailing();

            foreach (var (modId, handler) in snapshot)
                ModFaults.RunForMod(modId, "fail handler", () => handler(failing));

            if (!failing.IsPrevented)
                return true;

            prevented_fails.AddOrUpdate(player, failing);
            Unsubmit(player, "a mod stopped it from failing");
            return false;
        }

        /// <summary>
        ///     leaves a play that already started unsubmitted, lazer skips submitting without a token the same as when it couldn't get one
        /// </summary>
        public static void Unsubmit(object player, string reason)
        {
            if (!unsubmitted_mid_play.TryGetValue(player, out _))
            {
                unsubmitted_mid_play.AddOrUpdate(player, reason);
                LoaderLog.Info($"Not submitting this play: {reason}.");
            }

            if (player is SubmittingPlayer submitting)
                submitting.token = null;
        }

        public static string? UnsubmittedMidPlay(object player) => unsubmitted_mid_play.TryGetValue(player, out string? reason) ? reason : null;

        /// <summary>false when a handler cancelled the play</summary>
        public static bool Begin()
        {
            (string ModId, Action<PlayStarting> Handler)[] snapshot;

            lock (handlers)
                snapshot = [.. handlers];

            var starting = new PlayStarting();

            foreach (var (modId, handler) in snapshot)
                ModFaults.RunForMod(modId, "play starting handler", () => handler(starting));

            Current = starting.IsCancelled ? null : starting;
            return !starting.IsCancelled;
        }

        public static void End() => Current = null;

        public static void Tag(object player, PlayStarting starting) => started.AddOrUpdate(player, starting);

        /// <summary>
        ///     cuts a player's fresh beatmap down when a mod asked for the play to start later, every retry makes a new one
        /// </summary>
        public static void PrepareStart(object player, IBeatmap beatmap)
        {
            if (started.TryGetValue(player, out var starting) && starting.StartTime is { } time && LateStart.Trim(beatmap, time.TotalMilliseconds))
                late_starts.AddOrUpdate(player, new StrongBox<double>(time.TotalMilliseconds));
        }

        /// <summary>where the player's gameplay starts in milliseconds, null for the beginning</summary>
        public static double? StartTimeFor(object player) => late_starts.TryGetValue(player, out var start) ? start.Value : null;

        /// <summary>
        ///     why the player's play mustn't be submitted, null to submit it
        ///     if the check breaks, plays aren't submitted while any mod could have asked
        /// </summary>
        public static string? Unsubmitted(object player)
        {
            bool anyHandlers;

            lock (handlers)
                anyHandlers = handlers.Count > 0;

            return FeatureRegistry.Run(Feature.PlayStarting, anyHandlers ? broken_check : null,
                _ => started.TryGetValue(player, out var starting) ? starting.SuppressionReason : null);
        }

        internal static void Clear()
        {
            lock (handlers)
                handlers.Clear();

            lock (failing_handlers)
                failing_handlers.Clear();

            Current = null;
            onScreen = null;
        }
    }
}
