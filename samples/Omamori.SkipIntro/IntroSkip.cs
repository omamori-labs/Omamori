using HarmonyLib;
using osu.Framework.Bindables;
using osu.Framework.Screens;
using osu.Game.Screens.Menu;

namespace Omamori.SkipIntro
{
    [HarmonyPatch(typeof(IntroScreen), nameof(IntroScreen.PrepareMenuLoad))]
    internal static class IntroSkip
    {
        public static IBindable<bool>? Enabled { get; set; }

        [HarmonyPostfix]
        private static void postfix(IntroScreen __instance)
        {
            if (Enabled?.Value != true)
                return;

            __instance.Schedule(() =>
            {
                if (__instance.DidLoadMenu || !__instance.IsCurrentScreen())
                    return;

                __instance.StartTrack();
                __instance.LoadMenu();
            });
        }
    }
}
