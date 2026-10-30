using System.Collections.Generic;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Core.Features
{
    internal static class HookCheck
    {
        /// <summary>
        ///     an undeclared hook counts as optional and is hinted once, an unavailable one is skipped
        /// </summary>
        public static bool CanRegister(ModManifest manifest, IModLogger log, HashSet<Hook> hinted, Hook hook, string what)
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
