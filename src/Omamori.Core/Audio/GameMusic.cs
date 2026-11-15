using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Omamori.API;
using Omamori.Core.Details;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Overlays;

namespace Omamori.Core.Audio
{
    /// <summary>
    ///     the song is copied on the update thread when the track changes, realm objects can't be read from the threads mods call from
    /// </summary>
    internal sealed class GameMusic : IGameMusic
    {
        // set once the game has loaded
        private static OsuGame? game;

        private static volatile BeatmapDetails? current;

        // what each mod is holding the music down with, let go when it's turned off or gameplay starts
        private static readonly Dictionary<string, List<ModDuck>> ducks = [];

        private readonly ModManifest manifest;
        private readonly HookCheck hooks;

        public GameMusic(ModManifest manifest, IModLogger log)
        {
            this.manifest = manifest;
            hooks = new HookCheck(manifest, log);
        }

        public BeatmapDetails? Current => readable(out _) ? current : null;

        public bool IsPlaying => readable(out var music) && music.IsPlaying;

        public TimeSpan Position => readable(out var music) ? GameDetails.Time(music.CurrentTrack.CurrentTime) : TimeSpan.Zero;

        public TimeSpan Length => readable(out var music) ? GameDetails.Time(music.CurrentTrack.Length) : TimeSpan.Zero;

        public bool Pause() => control(m => m.Stop(requestedByUser: true));

        public bool Resume() => control(m => m.Play(requestedByUser: true));

        public bool NextTrack() => control(m => m.NextTrack());

        public bool PreviousTrack() => control(m => m.PreviousTrack());

        public bool SeekTo(TimeSpan position) => control(m => m.SeekTo(position.TotalMilliseconds));

        public IDisposable Duck()
        {
            var duck = new ModDuck(manifest.Id);

            if (!usable(out var g))
                return duck;

            lock (ducks)
            {
                if (!ducks.TryGetValue(manifest.Id, out var mine))
                    ducks[manifest.Id] = mine = [];

                mine.Add(duck);
            }

            schedule(g, () => duck.Start(g.MusicController));
            return duck;
        }

        public static void Attach(OsuGame loaded)
        {
            game = loaded;
            current = details(loaded.Beatmap.Value);

            loaded.MusicController.TrackChanged += (beatmap, _) => FeatureRegistry.Run(Feature.Audio, () => current = details(beatmap));

            // gameplay takes the track over, the music is the map then
            loaded.MusicController.AllowTrackControl.BindValueChanged(e =>
            {
                if (!e.NewValue)
                    letGo(null);
            });

            ModFaults.Faulted -= onFaulted;
            ModFaults.Faulted += onFaulted;
        }

        internal static void Clear()
        {
            ModFaults.Faulted -= onFaulted;
            letGo(null);
            current = null;
            game = null;
        }

        private static BeatmapDetails? details(WorkingBeatmap? beatmap) =>
            beatmap is null or DummyWorkingBeatmap ? null : GameDetails.Beatmap(beatmap.BeatmapInfo);

        // raised on whichever thread the mod threw, the ducks are let go on the update thread
        private static void onFaulted(string id) => game?.Schedule(() => letGo(id));

        private static void letGo(string? id)
        {
            List<ModDuck> released;

            lock (ducks)
            {
                released = [.. ducks.Where(d => id == null || d.Key == id).SelectMany(d => d.Value)];

                foreach (string key in ducks.Keys.Where(k => id == null || k == id).ToList())
                    ducks.Remove(key);
            }

            foreach (var duck in released)
                duck.Dispose();
        }

        private bool control(Action<MusicController> action)
        {
            if (!usable(out var g) || g.MusicController is not { } music || !music.AllowTrackControl.Value)
                return false;

            // checked again on the update thread, gameplay may have started in between
            schedule(g, () =>
            {
                if (music.AllowTrackControl.Value)
                    action(music);
            });

            return true;
        }

        private bool usable([NotNullWhen(true)] out OsuGame? loaded)
        {
            loaded = game;
            return loaded != null && hooks.CanRegister(Hook.Audio, "music");
        }

        // reading doesn't count as using the hook, so it never logs about an undeclared one
        private static bool readable([NotNullWhen(true)] out MusicController? music)
        {
            music = game?.MusicController;
            return music != null && FeatureRegistry.IsAvailable(Hook.Audio);
        }

        private static void schedule(OsuGame g, Action action) => g.Schedule((Action)(() => FeatureRegistry.Run(Feature.Audio, action)));

        /// <summary>
        ///     handed out straight away, the duck itself starts and ends on the update thread
        /// </summary>
        private sealed class ModDuck : IDisposable
        {
            private readonly string modId;
            private IDisposable? lazerDuck;
            private volatile bool disposed;

            public ModDuck(string modId)
            {
                this.modId = modId;
            }

            public void Start(MusicController? music)
            {
                if (disposed || music is not { AllowTrackControl.Value: true })
                    return;

                lazerDuck = music.Duck();
            }

            public void Dispose()
            {
                if (disposed)
                    return;

                disposed = true;

                lock (ducks)
                {
                    if (ducks.TryGetValue(modId, out var mine))
                        mine.Remove(this);
                }

                if (game is { } g)
                    g.Schedule(() => lazerDuck?.Dispose());
                else
                    lazerDuck?.Dispose();
            }
        }
    }
}
