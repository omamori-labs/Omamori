using Omamori.API;

namespace Omamori.Core
{
    // no manifest when it couldn't be read, or another mod has the same id
    internal sealed record SkippedMod(string Name, string Reason, ModManifest? Manifest = null);
}
