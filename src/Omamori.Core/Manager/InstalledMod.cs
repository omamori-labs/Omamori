using System;
using System.Linq;
using Omamori.API;
using Omamori.Core.Loading;
using Omamori.Core.Safety;

namespace Omamori.Core.Manager
{
    // no manifest when it couldn't be read or another mod has its id, it can't be switched
    internal sealed record InstalledMod(string Label, ModManifest? Manifest, bool OnAtLaunch, string? Problem)
    {
        public string? Id => Manifest?.Id;

        public bool Patches => Manifest != null && ManifestRules.DeclaresPatches(Manifest.Capabilities);

        public bool Enabled => Manifest != null && ModLoader.Switches?.IsEnabled(Manifest.Id) != false;

        public ModStatus Status =>
            Problem != null ? ModStatus.Problem
            : Id != null && ModFaults.IsFaulted(Id) ? ModStatus.Crashed
            : !OnAtLaunch ? ModStatus.Off
            : ModStatus.On;

        public bool HasProblem => Status is ModStatus.Problem or ModStatus.Crashed;

        /// <summary>skipped by the crash history and not retried yet</summary>
        public bool CanRetry => Id != null && Problem != null && !InstalledModList.IsRetried(Id) && ModFaults.History?.Skips(Id) == true;

        public bool Pending => (Manifest != null && Enabled != OnAtLaunch) || (Id != null && InstalledModList.IsRetried(Id));

        /// <summary>a pending change wins over what happened this session, null when there's nothing to say</summary>
        public string? StatusText
        {
            get
            {
                if (Pending)
                    return Enabled ? "Loads next time osu! starts." : "Turns off next time osu! starts.";

                if (Status == ModStatus.Crashed)
                    return "Crashed and was turned off for this session.";

                return string.IsNullOrEmpty(Problem) ? null : $"{char.ToUpperInvariant(Problem[0])}{Problem[1..]}.";
            }
        }

        public bool Matches(ModFilter filter) => filter switch
        {
            ModFilter.On => Status == ModStatus.On,
            ModFilter.Off => Status == ModStatus.Off,
            ModFilter.Problems => HasProblem,
            _ => true,
        };

        /// <summary>every term has to be somewhere in the name, id, author or description</summary>
        public bool Matches(string[] searchTerms)
        {
            string haystack = string.Join(' ', Label, Manifest?.Id, Manifest?.Author, Manifest?.Description);
            return searchTerms.All(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}
