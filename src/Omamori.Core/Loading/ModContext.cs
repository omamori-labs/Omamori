using System.Linq;
using Omamori.API;

namespace Omamori.Core
{
    internal sealed class ModContext : IModContext
    {
        public ModManifest Manifest { get; }
        public string ModDirectory { get; }
        public IModLogger Log { get; }
        public IUiHooks Ui { get; }
        public IGameEvents Events { get; }
        public IModSettings Settings { get; }

        public ModContext(ModManifest manifest, string modDirectory)
        {
            Manifest = manifest;
            ModDirectory = modDirectory;
            Log = new ModLogger(manifest.Id);
            Ui = new UiHooks(manifest, Log);
            Events = new GameEvents(manifest, Log);
            Settings = new ModConfig(manifest, ModConfig.PathFor(CoreEntry.ConfigDirectory, manifest.Id));
        }

        public bool IsHookAvailable(Hook hook) => FeatureRegistry.IsAvailable(hook);

        public bool IsModLoaded(string id)
        {
            var target = ModLoader.Get(id);
            if (target == null || ModFaults.IsFaulted(id))
                return false;

            var dependency = Manifest.Dependencies.FirstOrDefault(d => d.Id == id);
            return dependency == null || DependencyResolver.VersionProblem(dependency, target.Manifest) == null;
        }
    }
}
