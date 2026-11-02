using System;
using System.Text;
using Omamori.API;
using osu.Framework.Bindables;

namespace Omamori.NowPlaying
{
    public sealed class NowPlayingMod : IMod
    {
        private const int max_image_bytes = 256;

        private readonly Status status = new Status();
        private PresenceStatus? last;
        private IModContext context = null!;

        private IBindable<bool> enabled = null!;
        private IBindable<string> details = null!;
        private IBindable<string> state = null!;
        private IBindable<LargeImage> largeImage = null!;
        private IBindable<string> largeImageUrl = null!;
        private IBindable<string> largeImageText = null!;
        private IBindable<SmallImage> smallImage = null!;
        private IBindable<string> smallImageUrl = null!;
        private IBindable<string> smallImageText = null!;

        public void Load(IModContext context)
        {
            this.context = context;
            var keepOsus = new SettingOptions { Description = $"Use any of {Status.PLACEHOLDERS}, leave it empty to keep osu!'s." };
            var url = new SettingOptions { Description = "An https link to an image." };

            context.Settings.Note("Shows on Discord while playing, on results and in the editor, when osu!'s Discord Rich Presence setting is Full.");
            enabled = context.Settings.Toggle("enabled", "Change my Discord status", true);

            context.Settings.Header("Text");
            details = context.Settings.Text("format", "Top line", "{artist} - {title} [{diff}] {mods}", keepOsus);
            state = context.Settings.Text("state", "Bottom line", options: keepOsus);

            context.Settings.Header("Large image");
            largeImage = context.Settings.Choice("large-image", "Image", LargeImage.Cover);
            largeImageUrl = context.Settings.Text("large-image-url", "Custom image link", options: url);
            largeImageText = context.Settings.Text("large-image-text", "Hover text", options: keepOsus);

            context.Settings.Header("Small image");
            smallImage = context.Settings.Choice("small-image", "Image", SmallImage.Osu);
            smallImageUrl = context.Settings.Text("small-image-url", "Custom image link", options: url);
            smallImageText = context.Settings.Text("small-image-text", "Hover text", "{stars}★ · {bpm} BPM", keepOsus);

            void watch<T>(IBindable<T> setting) => setting.BindValueChanged(_ => update(() => { }));

            watch(enabled);
            watch(details);
            watch(state);
            watch(largeImage);
            watch(largeImageUrl);
            watch(largeImageText);
            watch(smallImage);
            watch(smallImageUrl);
            watch(smallImageText);

            context.Events.OnScreenChanged(change => update(() => status.Screen = change.Screen));
            context.Events.OnBeatmapChanged(beatmap => update(() => status.Beatmap = beatmap));
            context.Events.OnModsChanged(mods => update(() => status.Mods = mods));
            context.Events.OnPlayStarted(play => update(() => status.Play = play));
            context.Events.OnPlayEnded(_ => update(() => status.Play = null));
        }

        private void update(Action change)
        {
            change();

            var next = build();
            if (next == last)
                return;

            last = next;

            if (next == null)
                context.Presence.Clear();
            else
                context.Presence.Set(next);
        }

        private PresenceStatus? build()
        {
            if (!enabled.Value || status.Shown() is not var (beatmap, mods))
                return null;

            string? fill(string format) => string.IsNullOrWhiteSpace(format) ? null : Status.Fill(format, beatmap, mods);

            return new PresenceStatus
            {
                Details = fill(details.Value),
                State = fill(state.Value),
                LargeImage = largeImage.Value switch
                {
                    LargeImage.Cover when beatmap.SetOnlineId is { } set => $"https://assets.ppy.sh/beatmaps/{set}/covers/list@2x.jpg",
                    LargeImage.Custom => link(largeImageUrl.Value),
                    _ => null,
                },
                LargeImageText = fill(largeImageText.Value),
                SmallImage = smallImage.Value == SmallImage.Custom ? link(smallImageUrl.Value) : null,
                SmallImageText = fill(smallImageText.Value),
            };
        }

        // discord refuses anything else, and an overlong one would make Set throw and turn the mod off
        private static string? link(string value) =>
            Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && Encoding.UTF8.GetByteCount(uri.AbsoluteUri) <= max_image_bytes
                ? uri.AbsoluteUri
                : null;
    }
}
