using DiscordRPC;
using HarmonyLib;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Presence;
using osu.Desktop;

namespace Omamori.Core.Patching
{
    // osu! keeps discord conn, privacy check and timestamps, mods only replace text and images
    internal static partial class Patches
    {
        [Feature(Feature.DiscordPresence)]
        [HarmonyPatch(typeof(DiscordRichPresence), nameof(DiscordRichPresence.LoadComplete))]
        internal static class DiscordPresenceRefresh
        {
            [HarmonyPostfix]
            private static void postfix(DiscordRichPresence __instance)
            {
                FeatureRegistry.Run(Feature.DiscordPresence, () => ModPresence.Attach(() => __instance.Schedule(__instance.schedulePresenceUpdate)));
            }
        }

        // osu! only sets the large image once
        [Feature(Feature.DiscordPresence)]
        [HarmonyPatch(typeof(DiscordRichPresence), nameof(DiscordRichPresence.updatePresence))]
        internal static class DiscordPresenceStatus
        {
            private static Assets? osuAssets;

            [HarmonyPrefix]
            private static void prefix(DiscordRichPresence __instance)
            {
                FeatureRegistry.Run(Feature.DiscordPresence, () => Restore(__instance.presence));
            }

            // hidden is privacy Limited or do not disturb
            [HarmonyPostfix]
            private static void postfix(DiscordRichPresence __instance, bool hideIdentifiableInformation)
            {
                FeatureRegistry.Run(Feature.DiscordPresence, () => Apply(__instance.presence, hideIdentifiableInformation ? null : ModPresence.Current()));
            }

            internal static void Restore(RichPresence presence)
            {
                if (osuAssets is { } saved)
                    presence.Assets = copy(saved);
            }

            internal static void Apply(RichPresence presence, PresenceStatus? status)
            {
                presence.Assets ??= new Assets();
                osuAssets = copy(presence.Assets);

                if (status == null)
                    return;

                if (status.Details != null)
                    presence.Details = DiscordRichPresence.clampLength(status.Details);
                if (status.State != null)
                    presence.State = DiscordRichPresence.clampLength(status.State);
                if (status.LargeImage != null)
                    presence.Assets.LargeImageKey = status.LargeImage;
                if (status.LargeImageText != null)
                    presence.Assets.LargeImageText = DiscordRichPresence.clampLength(status.LargeImageText);
                if (status.SmallImage != null)
                    presence.Assets.SmallImageKey = status.SmallImage;
                if (status.SmallImageText != null)
                    presence.Assets.SmallImageText = DiscordRichPresence.clampLength(status.SmallImageText);
            }

            internal static void Reset() => osuAssets = null;

            private static Assets copy(Assets assets) => new Assets
            {
                LargeImageKey = assets.LargeImageKey,
                LargeImageText = assets.LargeImageText,
                SmallImageKey = assets.SmallImageKey,
                SmallImageText = assets.SmallImageText,
            };
        }
    }
}
