using Omamori.API;

namespace OmamoriMod
{
    public class Mod : IMod
    {
        public void Load(IModContext context)
        {
            context.Log.Info("loaded");

            if (context.IsHookAvailable(Hook.MainMenu))
                context.Ui.AddToMainMenu(() => new Greeting(context.Manifest.Name));
        }
    }
}
