using Omamori.Core.Config;
using Omamori.Core.Events;
using Omamori.Core.Features;
using Omamori.Core.Input;
using Omamori.Core.Loading;
using Omamori.Core.ModUi;
using Omamori.Core.Notifications;
using Omamori.Core.Patching;
using Omamori.Core.Presence;
using Omamori.Core.Safety;
using Omamori.Core.Services;

namespace Omamori.Tests
{
    internal static class CoreState
    {
        /// <summary>
        ///     everything core keeps in statics, back to how it is before the game starts, so a test can't leak into the next
        /// </summary>
        public static void Reset()
        {
            foreach (var info in FeatureRegistry.ALL)
                FeatureRegistry.Set(info.Feature, FeatureState.Pending, null);

            ModLoader.Clear();
            ModFaults.Clear();
            ModFaults.History = null;

            ModConfig.Clear();
            GameEvents.Clear();
            UiHooks.Clear();
            ModInput.Clear();
            ModServices.Clear();
            ModPresence.Reset();
            NotificationSink.Reset();
            Patches.DiscordPresenceStatus.Reset();
        }
    }
}
