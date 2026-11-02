using System;
using Omamori.API;
using osu.Framework.Bindables;

namespace Omamori.NowPlaying
{
    public sealed class NowPlayingMod : IMod
    {
        private readonly Status status = new Status();
        private string last = string.Empty;
        private IModContext context = null!;
        private StatusFile file = null!;
        private IBindable<string> path = null!;

        public void Load(IModContext context)
        {
            this.context = context;
            file = new StatusFile(context);

            context.Settings.Header("Status");
            var format = context.Settings.Text("format", "Beatmap format", Status.DEFAULT_FORMAT, new SettingOptions { Description = $"Use any of {Status.PLACEHOLDERS}." });
            status.Format = format.Value;
            format.BindValueChanged(e => update(() => status.Format = e.NewValue));

            context.Settings.Header("Text file");
            context.Settings.Note("For an OBS text source, select 'Read from file' and pick the same file.");
            path = context.Settings.Text("path", "File to write to", options: new SettingOptions { Description = "A full path like C:\\obs\\nowplaying.txt, leave it empty to turn this off." });
            path.BindValueChanged(e => file.Write(e.NewValue, last));

            context.Events.OnScreenChanged(change => update(() => status.Screen = change.Screen));
            context.Events.OnBeatmapChanged(beatmap => update(() => status.Beatmap = beatmap));
            context.Events.OnModsChanged(mods => update(() => status.Mods = mods));
            context.Events.OnPlayStarted(play => update(() => status.Play = play));
            context.Events.OnPlayEnded(_ => update(() => status.Play = null));

            context.Events.OnGameExiting(() => file.WriteNow(path.Value, string.Empty));
        }

        private void update(Action change)
        {
            change();

            string text = status.Describe();
            if (text == last)
                return;

            last = text;
            context.Log.Info(text);
            file.Write(path.Value, text);
        }
    }
}
