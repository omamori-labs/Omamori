using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Audio.Sample;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Overlays;

namespace Omamori.Core.Audio
{
    internal sealed class ModAudio : IModAudio
    {
        // set once the game has loaded
        private static OsuGame? game;

        // update thread only, each mod's own sounds, freed when it's turned off
        private static readonly Dictionary<string, ISampleStore> sample_stores = [];

        // what each mod is holding the music down with, let go when it's turned off or gameplay starts
        private static readonly Dictionary<string, List<ModDuck>> ducks = [];

        private readonly ModManifest manifest;
        private readonly string directory;
        private readonly HookCheck hooks;

        public ModAudio(ModManifest manifest, string directory, IModLogger log)
        {
            this.manifest = manifest;
            this.directory = directory;
            hooks = new HookCheck(manifest, log);
        }

        public bool PlaySound(string file, double volume = 1)
        {
            if (!usable(out var g) || InsideFolder(directory, file) is not { } path)
                return false;

            string name = Path.GetRelativePath(directory, path).Replace('\\', '/');
            string id = manifest.Id;
            string folder = directory;

            schedule(g, () =>
            {
                if (ModFaults.IsFaulted(id))
                    return;

                if (!sample_stores.TryGetValue(id, out var store))
                    sample_stores[id] = store = g.Audio.GetSampleStore(new StorageBackedResourceStore(new NativeStorage(folder)));

                if (store.Get(name) is not { } sample)
                    return;

                var channel = sample.GetChannel();
                channel.Volume.Value = Math.Clamp(volume, 0, 1);
                channel.Play();
            });

            return true;
        }

        public IDisposable DuckMusic()
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

        public bool PauseMusic() => control(m => m.Stop(requestedByUser: true));

        public bool ResumeMusic() => control(m => m.Play(requestedByUser: true));

        public bool NextTrack() => control(m => m.NextTrack());

        public bool PreviousTrack() => control(m => m.PreviousTrack());

        public bool SeekMusic(TimeSpan position) => control(m => m.SeekTo(position.TotalMilliseconds));

        /// <summary>the full path when <paramref name="file"/> exists inside <paramref name="folder"/>, null when it's missing or leads out of it</summary>
        public static string? InsideFolder(string folder, string file)
        {
            string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, file));
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            return path.StartsWith(root, comparison) && File.Exists(path) ? path : null;
        }

        public static void Attach(OsuGame loaded)
        {
            game = loaded;

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

            foreach (var store in sample_stores.Values)
                store.Dispose();

            sample_stores.Clear();
            game = null;
        }

        // raised on whichever thread the mod threw, cleanup goes to the update thread
        private static void onFaulted(string id) => game?.Schedule(() =>
        {
            letGo(id);

            if (sample_stores.Remove(id, out var store))
                store.Dispose();
        });

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

        private bool usable([NotNullWhen(true)] out OsuGame? current)
        {
            current = game;
            return current != null && hooks.CanRegister(Hook.Audio, "audio");
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
