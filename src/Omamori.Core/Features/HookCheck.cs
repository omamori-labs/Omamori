using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Core.Features
{
    /// <summary>
    ///     one per mod service, so each service hints about its own undeclared hooks once
    /// </summary>
    internal sealed class HookCheck
    {
        private readonly ModManifest manifest;
        private readonly IModLogger log;
        private readonly HashSet<Hook> hinted = [];

        public HookCheck(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            this.log = log;
        }

        /// <summary>
        ///     an undeclared hook counts as optional and is hinted once, an unavailable one is skipped
        /// </summary>
        public bool CanRegister(Hook hook, string what)
        {
            if (!manifest.Hooks.ContainsKey(hook) && hinted.Add(hook))
                log.Info($"uses the {hook} hook without declaring it in {ModLoader.MANIFEST_FILENAME}, treating it as optional.");

            if (FeatureRegistry.IsAvailable(hook))
                return true;

            log.Info($"{hook} hook isn't available on this osu! version, skipping this {what}.");
            return false;
        }
    }
}
