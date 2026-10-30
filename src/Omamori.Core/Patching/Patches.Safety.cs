using System;
using HarmonyLib;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Game;
using osu.Game.Utils;

namespace Omamori.Core.Patching
{
    // keeping mods' crashes from crashing osu! or reaching ppy
    internal static partial class Patches
    {
        /// <summary>
        ///     crashes involving omamori or a mod aren't sent to ppy
        /// </summary>
        [Feature(Feature.CrashReportFilter)]
        [HarmonyPatch(typeof(SentryLogger), nameof(SentryLogger.shouldSubmitException))]
        internal static class CrashReportFilter
        {
            [HarmonyPostfix]
            private static void postfix(Exception exception, ref bool __result)
            {
                __result = FeatureRegistry.Run(Feature.CrashReportFilter, __result, submit =>
                {
                    if (!submit || CrashAttribution.Involvement(exception) is not string involved)
                        return submit;

                    LoaderLog.Info($"Not reporting {exception.GetType().Name} to ppy, it involves {involved}.");
                    return false;
                });
            }
        }

        /// <summary>
        ///     an exception from a mod turns that mod off instead of counting towards osu!'s crash limit
        /// </summary>
        [Feature(Feature.ModContainment)]
        [HarmonyPatch(typeof(OsuGameBase), nameof(OsuGameBase.onExceptionThrown))]
        internal static class ModContainment
        {
            [HarmonyPrefix]
            private static bool prefix(OsuGameBase __instance, Exception ex, ref bool __result)
            {
                bool contained = FeatureRegistry.Run(Feature.ModContainment, false, _ => ModFaults.TryContain(ex, __instance.Host.UpdateThread.Scheduler));

                if (contained)
                    __result = true;

                return !contained;
            }
        }
    }
}
