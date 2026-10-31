using System.Linq;
using Omamori.API;

namespace Omamori.Core.Loading
{
    /// <summary>
    ///     mods get their own copy, so changing it can't touch the one core blames faults and files registrations by
    /// </summary>
    internal static class ManifestCopy
    {
        public static ModManifest Of(ModManifest manifest) => new ModManifest
        {
            Id = manifest.Id,
            Name = manifest.Name,
            Version = manifest.Version,
            Author = manifest.Author,
            Description = manifest.Description,
            Entry = manifest.Entry,
            ApiVersion = manifest.ApiVersion,
            Capabilities = [.. manifest.Capabilities],
            Hooks = manifest.Hooks.ToDictionary(),
            Dependencies = [.. manifest.Dependencies.ConvertAll(d => new ModDependency { Id = d.Id, MinVersion = d.MinVersion, Optional = d.Optional })],
        };
    }
}
