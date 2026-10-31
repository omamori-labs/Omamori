using Omamori.API;
using osu.Framework.Bindables;
using osu.Framework.Input.Bindings;

namespace OmamoriMod
{
    public class Mod : IMod
    {
        public void Load(IModContext context)
        {
            context.Log.Info("loaded");

            // shows on the mod's settings tab in omamori's mod manager, changes reach the label live
            var text = context.Settings.Text("text", "Greeting", $"hello from {context.Manifest.Name}");

            var toggle = context.Settings.KeyBinding("toggle", "Show or hide the greeting", new ModKeys(InputKey.Control, InputKey.G));
            var shown = new BindableBool(true);

            context.Input.OnPressed(toggle, () => shown.Value = !shown.Value);

            if (context.IsHookAvailable(Hook.MainMenu))
                context.Ui.AddToMainMenu(() => new Greeting(text, shown));

            // read-only, the result arrives after the play
            context.Events.OnPlayEnded(result =>
            {
                if (result.Score is { } score && !result.Play.IsReplay)
                    context.Notifications.Post($"finished with {score.Rank}, {score.Accuracy:P2}");
            });
        }
    }
}
