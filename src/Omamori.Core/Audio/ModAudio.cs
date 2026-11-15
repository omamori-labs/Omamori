using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Safety;
using osu.Framework.Audio.Sample;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game;

namespace Omamori.Core.Audio
{
    internal sealed class ModAudio : IModAudio
    {
        // set once the game has loaded
        private static OsuGame? game;

        // update thread only, each mod's own sounds, freed when it's turned off
        private static readonly Dictionary<string, ISampleStore> sample_stores = [];

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

            ModFaults.Faulted -= onFaulted;
            ModFaults.Faulted += onFaulted;
        }

        internal static void Clear()
        {
            ModFaults.Faulted -= onFaulted;

            foreach (var store in sample_stores.Values)
                store.Dispose();

            sample_stores.Clear();
            game = null;
        }

        // raised on whichever thread the mod threw, cleanup goes to the update thread
        private static void onFaulted(string id) => game?.Schedule(() =>
        {
            if (sample_stores.Remove(id, out var store))
                store.Dispose();
        });

        private bool usable([NotNullWhen(true)] out OsuGame? current)
        {
            current = game;
            return current != null && hooks.CanRegister(Hook.Audio, "audio");
        }

        private static void schedule(OsuGame g, Action action) => g.Schedule((Action)(() => FeatureRegistry.Run(Feature.Audio, action)));
    }
}
