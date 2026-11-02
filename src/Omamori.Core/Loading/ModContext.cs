using System.IO;
using System.Linq;
using Omamori.API;
using Omamori.Core.Config;
using Omamori.Core.Events;
using Omamori.Core.Features;
using Omamori.Core.Input;
using Omamori.Core.ModUi;
using Omamori.Core.Notifications;
using Omamori.Core.Safety;
using Omamori.Core.Services;
using osu.Framework.Platform;

namespace Omamori.Core.Loading
{
    internal sealed class ModContext : IModContext
    {
        private readonly ModManifest manifest;
        private readonly string dataDirectory;
        private readonly GameHost? host;

        public ModManifest Manifest { get; }
        public string ModDirectory { get; }

        public string DataDirectory
        {
            get
            {
                Directory.CreateDirectory(dataDirectory);
                return dataDirectory;
            }
        }

        public IModLogger Log { get; }
        public IUiHooks Ui { get; }
        public IGameEvents Events { get; }
        public IModSettings Settings { get; }
        public IModNotifications Notifications { get; }
        public IModInput Input { get; }
        public IModServices Services { get; }

        public ModContext(ModManifest manifest, string modDirectory, string dataDirectory, GameHost? host = null)
        {
            this.manifest = manifest;
            this.dataDirectory = dataDirectory;
            this.host = host;
            Manifest = ManifestCopy.Of(manifest);
            ModDirectory = modDirectory;
            Log = new ModLogger(manifest.Id);
            Ui = new UiHooks(manifest, Log);
            Events = new GameEvents(manifest, Log);
            Settings = new ModConfig(manifest, ModConfig.PathFor(CoreEntry.ConfigDirectory, manifest.Id));
            Notifications = new ModNotifications(manifest, Log);
            Input = new ModInput(manifest, Log);
            Services = new ModServices(manifest, Log);
        }

        public bool IsHookAvailable(Hook hook) => FeatureRegistry.IsAvailable(hook);

        public bool IsModLoaded(string id)
        {
            var target = ModLoader.Get(id);
            if (target == null || ModFaults.IsFaulted(id))
                return false;

            var dependency = manifest.Dependencies.FirstOrDefault(d => d.Id == id);
            return dependency == null || DependencyResolver.VersionProblem(dependency, target.Manifest) == null;
        }

        // a file is shown rather than opened (prevent rce)
        public bool ShowInFileBrowser(string path)
        {
            if (host == null)
                return false;

            if (Directory.Exists(path))
                return host.OpenFileExternally(path);

            return File.Exists(path) && host.PresentFileExternally(path);
        }
    }
}
