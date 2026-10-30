using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Omamori.API;

namespace Omamori.Loader
{
    internal static class ModLoader
    {
        public const string MANIFEST_FILENAME = "mod.json";

        private static readonly JsonSerializerOptions json_options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        public static readonly List<LoadedMod> LOADED = new List<LoadedMod>();

        public static void LoadAll(string modsDirectory)
        {
            if (!Directory.Exists(modsDirectory))
            {
                Directory.CreateDirectory(modsDirectory);
                LoaderLog.Info($"Created mods directory at {modsDirectory}.");
                return;
            }

            foreach (string modDirectory in Directory.GetDirectories(modsDirectory).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var mod = load(modDirectory);
                    if (mod != null)
                        LOADED.Add(mod);
                }
                catch (Exception e)
                {
                    LoaderLog.Error($"Failed to load mod from {Path.GetFileName(modDirectory)}.", e);
                }
            }

            LoaderLog.Info($"Loaded {LOADED.Count} mod(s).");
        }

        private static LoadedMod? load(string modDirectory)
        {
            string manifestPath = Path.Combine(modDirectory, MANIFEST_FILENAME);

            if (!File.Exists(manifestPath))
            {
                LoaderLog.Info($"Skipping {Path.GetFileName(modDirectory)}: no {MANIFEST_FILENAME}.");
                return null;
            }

            var manifest = JsonSerializer.Deserialize<ModManifest>(File.ReadAllText(manifestPath), json_options)
                           ?? throw new InvalidDataException("Manifest is empty.");

            string? problem = validate(manifest, modDirectory);

            if (problem != null)
            {
                LoaderLog.Error($"Refusing to load {manifest.Id}: {problem}");
                return null;
            }

            var assembly = Assembly.LoadFrom(Path.Combine(modDirectory, manifest.Entry));

            var entryTypes = assembly.GetTypes().Where(t => typeof(IMod).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false }).ToList();

            if (entryTypes.Count != 1)
            {
                LoaderLog.Error($"Refusing to load {manifest.Id}: expected exactly one {nameof(IMod)} implementation, found {entryTypes.Count}.");
                return null;
            }

            var instance = (IMod)Activator.CreateInstance(entryTypes[0])!;
            var context = new ModContext(manifest, modDirectory);

            instance.Load(context);

            LoaderLog.Info($"Loaded {manifest.Name} {manifest.Version} by {manifest.Author} [{string.Join(", ", manifest.Capabilities)}].");
            return new LoadedMod(manifest, instance);
        }

        private static string? validate(ModManifest manifest, string modDirectory)
        {
            if (string.IsNullOrWhiteSpace(manifest.Id))
                return "manifest has no id.";

            if (LOADED.Any(m => m.Manifest.Id == manifest.Id))
                return "a mod with the same id is already loaded.";

            if (manifest.ApiVersion != ApiInfo.VERSION)
                return $"built for API {manifest.ApiVersion}, loader provides {ApiInfo.VERSION}.";

            if (manifest.Capabilities.Count == 0)
                return "manifest declares no capabilities.";

            if (string.IsNullOrWhiteSpace(manifest.Entry) || Path.IsPathRooted(manifest.Entry) || manifest.Entry.Contains(".."))
                return "entry must be a file name inside the mod directory.";

            if (!File.Exists(Path.Combine(modDirectory, manifest.Entry)))
                return $"entry assembly {manifest.Entry} not found.";

            return null;
        }
    }

    internal record LoadedMod(ModManifest Manifest, IMod Instance);

    internal class ModContext : IModContext
    {
        public ModManifest Manifest { get; }
        public string ModDirectory { get; }
        public IModLogger Log { get; }
        public IUiHooks Ui { get; }

        public ModContext(ModManifest manifest, string modDirectory)
        {
            Manifest = manifest;
            ModDirectory = modDirectory;
            Log = new ModLogger(manifest.Id);
            Ui = new UiHooks(manifest.Id);
        }
    }
}
