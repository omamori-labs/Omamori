using Omamori.API;

namespace Omamori.Core
{
    // Loader is kept so the mod's load context lives as long as the mod
    internal sealed record LoadedMod(ModManifest Manifest, IMod Instance, McMaster.NETCore.Plugins.PluginLoader Loader);
}
