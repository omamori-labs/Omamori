using Omamori.API;

namespace Omamori.Core
{
    // no manifest when it couldn't be read or another mod has its id, it can't be switched
    internal sealed record InstalledMod(string Label, ModManifest? Manifest, bool OnAtLaunch, string? Problem)
    {
        public string? Id => Manifest?.Id;

        public bool Enabled => Manifest != null && ModLoader.Switches?.IsEnabled(Manifest.Id) != false;

        public ModStatus Status =>
            Id != null && ModFaults.IsFaulted(Id) ? ModStatus.Crashed
            : Problem != null ? ModStatus.Problem
            : !OnAtLaunch ? ModStatus.Off
            : ModStatus.On;

        /// <summary>skipped by the crash history and not retried yet</summary>
        public bool CanRetry => Id != null && Problem != null && !InstalledModList.IsRetried(Id) && ModFaults.History?.Skips(Id) == true;

        public bool Pending => (Manifest != null && Enabled != OnAtLaunch) || (Id != null && InstalledModList.IsRetried(Id));

        public string? StatusText =>
            Pending ? Enabled ? "Loads next time osu! starts." : "Turns off next time osu! starts."
            : Status == ModStatus.Crashed ? "Crashed and was turned off for this session."
            : string.IsNullOrEmpty(Problem) ? null
            : $"{char.ToUpperInvariant(Problem[0])}{Problem[1..]}.";
    }
}
