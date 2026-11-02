using HarmonyLib;
using osu.Framework.Bindables;
using osu.Framework.Screens;
using osu.Game.Screens.Menu;

namespace Omamori.SkipIntro
{
    [HarmonyPatch(typeof(IntroScreen), nameof(IntroScreen.OnResuming))]
    internal static class OutroSkip
    {
        public static IBindable<bool>? Enabled { get; set; }

        [HarmonyPrefix]
        private static bool prefix(IntroScreen __instance)
        {
            if (Enabled?.Value != true)
                return true;

            __instance.Schedule(() =>
            {
                if (__instance.IsCurrentScreen())
                    __instance.Exit();
            });

            return false;
        }
    }
}
