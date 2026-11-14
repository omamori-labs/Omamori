using Omamori.API;

namespace Omamori.Core.Loading
{
    // Loader is kept so the mod's load context lives as long as the mod
    // Label is the candidate's, so a mod is called the same thing whether it loaded or not
    // the directory lets a mod turned off mid session be listed with the mods turned off at launch
    internal sealed record LoadedMod(ModManifest Manifest, IMod Instance, McMaster.NETCore.Plugins.PluginLoader Loader, string Label, string Directory = "");
}
