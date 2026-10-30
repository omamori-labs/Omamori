using Omamori.API;

namespace OmamoriMod
{
    public class Mod : IMod
    {
        public void Load(IModContext context)
        {
            context.Log.Info("loaded");

            // shows on the mod's settings tab in omamori's mod manager, changes reach the label live
            var text = context.Settings.Text("text", "Greeting", $"hello from {context.Manifest.Name}");

            if (context.IsHookAvailable(Hook.MainMenu))
                context.Ui.AddToMainMenu(() => new Greeting(text));

            // read-only, the result arrives after the play
            context.Events.OnPlayEnded(result =>
            {
                if (result.Score is { } score)
                    context.Log.Info($"finished a play with {score.Rank}, {score.Accuracy:P2}");
            });
        }
    }
}
