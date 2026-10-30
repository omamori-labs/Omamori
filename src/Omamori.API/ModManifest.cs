using System.Collections.Generic;

namespace Omamori.API
{
    public sealed class ModManifest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>name of mods assembly</summary>
        public string Entry { get; set; } = string.Empty;

        /// <summary>The <see cref="ApiInfo.VERSION"/> the mod was built for</summary>
        public string ApiVersion { get; set; } = string.Empty;

        public List<ModCapability> Capabilities { get; set; } = [];

        /// <summary>hooks the mod uses, undeclared ones count as optional</summary>
        public Dictionary<Hook, HookRequirement> Hooks { get; set; } = [];

        public List<ModDependency> Dependencies { get; set; } = [];
    }
}
