using Omamori.Core.Features;
using osu.Framework.Graphics;
using osu.Game.Overlays;

namespace Omamori.Core.Events.Sources
{
    /// <summary>
    ///     MusicController.IsPlaying has no event, so it's compared each frame
    /// </summary>
    internal sealed partial class MusicPlayingWatcher : Component
    {
        private readonly MusicController music;
        private bool playing;

        public MusicPlayingWatcher(MusicController music)
        {
            this.music = music;
        }

        protected override void Update()
        {
            base.Update();

            if (music.IsPlaying == playing)
                return;

            playing = music.IsPlaying;
            FeatureRegistry.Run(Feature.MusicEvents, () => GameEvents.Raise(GameEvent.MusicPlayingChanged, playing));
        }
    }
}
