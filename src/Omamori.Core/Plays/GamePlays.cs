using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;

namespace Omamori.Core.Plays
{
    internal sealed class GamePlays : IGamePlays
    {
        private static readonly List<(string ModId, Action<PlayStarting> Handler)> handlers = [];

        // keyed by the player, so a retry that makes a new one is still covered by the decision made on song select
#pragma warning disable IDE0028 // a weak table has no collection expression form
        private static readonly ConditionalWeakTable<object, PlayStarting> started = new ConditionalWeakTable<object, PlayStarting>();
#pragma warning restore IDE0028

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public GamePlays(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        /// <summary>what song select decided for the play it is starting right now, null outside of that</summary>
        public static PlayStarting? Current { get; private set; }

        public void OnStarting(Action<PlayStarting> handler)
        {
            if (!hooks.CanRegister(Hook.PlayStarting, "handler"))
                return;

            lock (handlers)
                handlers.Add((manifest.Id, handler));
        }

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

        public static bool IsSuppressed(object player, out string? reason)
        {
            reason = started.TryGetValue(player, out var starting) ? starting.SuppressionReason : null;
            return reason != null;
        }

        internal static void Clear()
        {
            lock (handlers)
                handlers.Clear();

            Current = null;
        }
    }
}
