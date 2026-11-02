using System;
using System.IO;
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
        private IBindable<bool> write = null!;
        private IBindable<string> path = null!;
        private string defaultPath = string.Empty;

        public void Load(IModContext context)
        {
            this.context = context;
            file = new StatusFile(context);

            context.Settings.Header("Status");
            var format = context.Settings.Text("format", "Beatmap format", Status.DEFAULT_FORMAT, new SettingOptions { Description = $"Use any of {Status.PLACEHOLDERS}." });
            status.Format = format.Value;
            format.BindValueChanged(e => update(() => status.Format = e.NewValue));

            context.Settings.Header("Text file");
            defaultPath = Path.Combine(context.DataDirectory, "nowplaying.txt");
            context.Settings.Note("For an OBS text source, select 'Read from file' and pick the status file.");
            write = context.Settings.Toggle("write", "Write the status to a file", true);
            path = context.Settings.Text("path", "File to write to", options: new SettingOptions { Description = "A full path like C:\\obs\\nowplaying.txt, leave it empty to use the mod's own folder." });
            context.Settings.Button("Status file", "Show in folder", showFile);
            write.BindValueChanged(e => file.Write(target(), e.NewValue ? last : string.Empty));
            path.BindValueChanged(_ => file.Write(target(), last));

            context.Events.OnScreenChanged(change => update(() => status.Screen = change.Screen));
            context.Events.OnBeatmapChanged(beatmap => update(() => status.Beatmap = beatmap));
            context.Events.OnModsChanged(mods => update(() => status.Mods = mods));
            context.Events.OnPlayStarted(play => update(() => status.Play = play));
            context.Events.OnPlayEnded(_ => update(() => status.Play = null));

            context.Events.OnGameExiting(() =>
            {
                if (write.Value)
                    file.WriteNow(target(), string.Empty);
            });
        }

        private void showFile()
        {
            string chosen = target();

            if (!context.ShowInFileBrowser(chosen) && !context.ShowInFileBrowser(Path.GetDirectoryName(chosen) ?? string.Empty))
                context.Notifications.PostError($"{chosen} doesn't exist yet, turn on writing the status to a file first.");
        }

        private string target() => string.IsNullOrWhiteSpace(path.Value) ? defaultPath : path.Value;

        private void update(Action change)
        {
            change();

            string text = status.Describe();
            if (text == last)
                return;

            last = text;
            context.Log.Info(text);

            if (write.Value)
                file.Write(target(), text);
        }
    }
}
