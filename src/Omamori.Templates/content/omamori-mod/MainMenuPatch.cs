using HarmonyLib;
using Omamori.API;
using osu.Game.Screens.Menu;

namespace OmamoriMod
{
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.OnEntering))]
    internal static class MainMenuPatch
    {
        public static IModLogger? Log { get; set; }

        [HarmonyPostfix]
        private static void postfix() => Log?.Info("main menu entered with a harmony patch");
    }
}
