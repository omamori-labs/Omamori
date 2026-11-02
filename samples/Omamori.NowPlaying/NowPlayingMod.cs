using System;
using Omamori.API;

namespace Omamori.NowPlaying
{
    public sealed class NowPlayingMod : IMod
    {
        private readonly Status status = new Status();
        private string last = string.Empty;

        public void Load(IModContext context)
        {
            context.Events.OnScreenChanged(change => update(context, () => status.Screen = change.Screen));
            context.Events.OnBeatmapChanged(beatmap => update(context, () => status.Beatmap = beatmap));
            context.Events.OnPlayStarted(play => update(context, () => status.Play = play));
            context.Events.OnPlayEnded(_ => update(context, () => status.Play = null));
        }

        private void update(IModContext context, Action change)
        {
            change();

            string text = status.Describe();
            if (text == last)
                return;

            last = text;
            context.Log.Info(text);
        }
    }
}
