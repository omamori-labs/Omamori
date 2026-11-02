using HarmonyLib;
using Omamori.API;

namespace Omamori.SkipIntro
{
    public sealed class SkipIntroMod : IMod
    {
        public void Load(IModContext context)
        {
            IntroSkip.Enabled = context.Settings.Toggle("enabled", "Skip the intro", true, new SettingOptions { Description = "Goes straight to the main menu when osu! starts." });
            OutroSkip.Enabled = context.Settings.Toggle("outro", "Skip the outro", true, new SettingOptions { Description = "Closes osu! straight away instead of saying goodbye first." });

            var harmony = new Harmony(context.Manifest.Id);
            harmony.CreateClassProcessor(typeof(IntroSkip)).Patch();
            harmony.CreateClassProcessor(typeof(OutroSkip)).Patch();
        }
    }
}
