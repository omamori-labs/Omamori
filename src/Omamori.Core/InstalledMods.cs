using System;
using System.Collections.Generic;
using System.Linq;
using Omamori.API;

namespace Omamori.Core
{
    internal enum ModStatus
    {
        On,
        Off,
        Problem,
        Crashed,
    }

    // no manifest when it couldn't be read or another mod has its id, it can't be switched
    internal sealed record InstalledMod(string Label, ModManifest? Manifest, bool OnAtLaunch, string? Problem)
    {
        public string? Id => Manifest?.Id;

        public bool Enabled => Manifest != null && ModLoader.Settings?.IsEnabled(Manifest.Id) != false;

        public ModStatus Status =>
            Id != null && ModFaults.IsFaulted(Id) ? ModStatus.Crashed
            : Problem != null ? ModStatus.Problem
            : !OnAtLaunch ? ModStatus.Off
            : ModStatus.On;

        /// <summary>skipped by the crash history and not retried yet</summary>
        public bool CanRetry => Id != null && Problem != null && !InstalledMods.IsRetried(Id) && ModFaults.History?.Skips(Id) == true;

        public bool Pending => (Manifest != null && Enabled != OnAtLaunch) || (Id != null && InstalledMods.IsRetried(Id));

        public string? StatusText =>
            Pending ? Enabled ? "Loads next time osu! starts." : "Turns off next time osu! starts."
            : Status == ModStatus.Crashed ? "Crashed and was turned off for this session."
            : string.IsNullOrEmpty(Problem) ? null
            : $"{char.ToUpperInvariant(Problem[0])}{Problem[1..]}.";
    }

    /// <summary>
    ///     what the settings list and the mod manager show, changes apply next launch since a mod's Load only runs once
    /// </summary>
    internal static class InstalledMods
    {
        private static readonly HashSet<string> retried = [];

        /// <summary>a switch or retry changed something, raised on the update thread</summary>
        public static event Action? Changed;

        public static bool Available => ModLoader.Settings != null;

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
            if (mod.Id == null || ModLoader.Settings is not { } settings || mod.Enabled == enabled)
                return;

            settings.SetEnabled(mod.Id, enabled);
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
