using System.Collections.ObjectModel;
using System.Linq;
using Omamori.API;

namespace Omamori.Core.Loading
{
    /// <summary>
    ///     mods get their own copy behind read-only wrappers, so casting a collection back to a list can't touch
    ///     the one core blames faults and files registrations by
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
            Capabilities = manifest.Capabilities.ToList().AsReadOnly(),
            Hooks = new ReadOnlyDictionary<Hook, HookRequirement>(manifest.Hooks.ToDictionary()),
            Dependencies = manifest.Dependencies.Select(d => new ModDependency { Id = d.Id, MinVersion = d.MinVersion, Optional = d.Optional }).ToList().AsReadOnly(),
        };
    }
}
