using System.Collections.Generic;

namespace Omamori.API
{
    /// <summary>
    ///     the mod's mod.json, read-only once loaded
    /// </summary>
    public sealed class ModManifest
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;

        /// <summary>the file name of the mod's assembly</summary>
        public string Entry { get; init; } = string.Empty;

        /// <summary>the <see cref="ApiInfo.VERSION"/> the mod was built for</summary>
        public string ApiVersion { get; init; } = string.Empty;

        public IReadOnlyList<ModCapability> Capabilities { get; init; } = [];

        /// <summary>hooks the mod uses, undeclared ones count as optional</summary>
        public IReadOnlyDictionary<Hook, HookRequirement> Hooks { get; init; } = new Dictionary<Hook, HookRequirement>();

        public IReadOnlyList<ModDependency> Dependencies { get; init; } = [];
    }
}
