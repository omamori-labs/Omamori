using System;
using System.Collections.Generic;
using System.Text;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;

namespace Omamori.Core.Presence
{
    /// <summary>
    ///     oldest first
    /// </summary>
    internal sealed class ModPresence : IModPresence
    {
        public const int MAX_IMAGE_BYTES = 256;

        private static readonly List<(string ModId, PresenceStatus Status)> set_by_mods = [];
        private static readonly Action<string> on_faulted = _ => Refresh();
        private static Action? refresh;

        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];
        private bool unavailable;

        public ModPresence(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        public void Set(PresenceStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);

            if (tooLong(status.LargeImage) || tooLong(status.SmallImage))
                throw new ArgumentException($"presence images can be at most {MAX_IMAGE_BYTES} bytes", nameof(status));

            if (ModFaults.IsFaulted(manifest.Id) || !canSet())
                return;

            lock (set_by_mods)
            {
                set_by_mods.RemoveAll(p => p.ModId == manifest.Id);
                set_by_mods.Add((manifest.Id, status));
            }

            Refresh();
        }

        public void Clear()
        {
            int removed;

            lock (set_by_mods)
                removed = set_by_mods.RemoveAll(p => p.ModId == manifest.Id);

            if (removed > 0)
                Refresh();
        }

        /// <summary>null leaves orig from osu!</summary>
        public static PresenceStatus? Current()
        {
            lock (set_by_mods)
            {
                for (int i = set_by_mods.Count - 1; i >= 0; i--)
                {
                    if (!ModFaults.IsFaulted(set_by_mods[i].ModId))
                        return set_by_mods[i].Status;
                }
            }

            return null;
        }

        /// <summary>
        ///     <paramref name="update"/> asks osu!'s presence to send again
        /// </summary>
        public static void Attach(Action update)
        {
            refresh = update;

            ModFaults.Faulted -= on_faulted;
            ModFaults.Faulted += on_faulted;
        }

        public static void Refresh() => refresh?.Invoke();

        internal static void Reset()
        {
            lock (set_by_mods)
                set_by_mods.Clear();

            ModFaults.Faulted -= on_faulted;
            refresh = null;
        }

        private static bool tooLong(string? image) => image != null && Encoding.UTF8.GetByteCount(image) > MAX_IMAGE_BYTES;

        private bool canSet()
        {
            if (unavailable && !FeatureRegistry.IsAvailable(Hook.DiscordPresence))
                return false;

            unavailable = !HookCheck.CanRegister(manifest, log, hinted, Hook.DiscordPresence, "presence");
            return !unavailable;
        }
    }
}
