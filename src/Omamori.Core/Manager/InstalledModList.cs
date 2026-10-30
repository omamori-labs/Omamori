using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.Core.Loading;
using Omamori.Core.Safety;

namespace Omamori.Core.Manager
{
    /// <summary>
    ///     what the settings list and the mod manager show, changes apply next launch since a mod's Load only runs once
    /// </summary>
    internal static class InstalledModList
    {
        private static readonly HashSet<string> retried = [];

        /// <summary>a switch or retry changed something, raised on the update thread</summary>
        public static event Action? Changed;

        public static bool Available => ModLoader.Switches != null;

        public static IReadOnlyList<InstalledMod> All() =>
        [
            .. ModLoader.LOADED.ToList().Select(m => new InstalledMod(string.IsNullOrWhiteSpace(m.Manifest.Name) ? m.Manifest.Id : m.Manifest.Name, m.Manifest, true, null))
                        .Concat(ModLoader.TURNED_OFF.Select(c => new InstalledMod(c.Label, c.Manifest, false, null)))
                        .Concat(ModLoader.SKIPPED.Select(s => new InstalledMod(s.Name, s.Manifest, s.Manifest != null, s.Reason)))
                        .OrderBy(m => m.Label, StringComparer.OrdinalIgnoreCase)
        ];

        public static int PendingChanges => All().Count(m => m.Pending);

        public static bool IsRetried(string id)
        {
            lock (retried)
                return retried.Contains(id);
        }

        public static void SetEnabled(InstalledMod mod, bool enabled)
        {
            if (mod.Id == null || ModLoader.Switches is not { } switches || mod.Enabled == enabled)
                return;

            switches.SetEnabled(mod.Id, enabled);
            Changed?.Invoke();
        }

        public static void Retry(InstalledMod mod)
        {
            if (!mod.CanRetry)
                return;

            ModFaults.History!.Forget(mod.Id!);

            lock (retried)
                retried.Add(mod.Id!);

            Changed?.Invoke();
        }
    }
}
