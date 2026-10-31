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

namespace Omamori.Core.Loading
{
    internal sealed class ModContext : IModContext
    {
        private readonly ModManifest manifest;

        public ModManifest Manifest { get; }
        public string ModDirectory { get; }
        public IModLogger Log { get; }
        public IUiHooks Ui { get; }
        public IGameEvents Events { get; }
        public IModSettings Settings { get; }
        public IModNotifications Notifications { get; }
        public IModInput Input { get; }
        public IModServices Services { get; }

        public ModContext(ModManifest manifest, string modDirectory)
        {
            this.manifest = manifest;
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
    }
}
