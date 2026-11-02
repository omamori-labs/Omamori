using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Features;
using Omamori.Core.Loading;
using Omamori.Core.ModUi;
using Omamori.Core.Safety;
using osu.Framework.Graphics.Containers;

namespace Omamori.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class ModLoaderTests
    {
        private string root = null!;
        private string mods = null!;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");
            mods = Path.Combine(root, "mods");
            Directory.CreateDirectory(mods);
        }

        [TearDown]
        public void TearDown()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);
            UiHooks.Clear();
            ModFaults.Clear();
            ModFaults.History = null;
            ModLoader.LOADED.Clear();
            ModLoader.TURNED_OFF.Clear();
            ModLoader.SKIPPED.Clear();
            ModLoader.Switches = null;

            try
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        [Test]
        public void LoadThatThrowsLeavesNothingRunning()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Active, null);

            var manifest = new ModManifest { Id = "test.load-throws", Name = "Load throws" };
            var loader = ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll"));

            Assert.Throws<InvalidOperationException>(() => ModLoader.RunLoad(manifest.Id, loader, () =>
            {
                new UiHooks(manifest, new ModLogger(manifest.Id)).AddGlobalOverlay(() => new Container());
                throw new InvalidOperationException("bad setting key");
            }));

            var overlay = new Container();
            UiHooks.Populate(Hook.GlobalOverlay, overlay);

            Assert.That(ModFaults.IsFaulted(manifest.Id), Is.True);
            Assert.That(overlay.Count, Is.Zero);
        }

        [Test]
        public void LoadThatWorksLeavesTheModOn()
        {
            var loader = ModIsolation.CreateLoader(Path.Combine(AppContext.BaseDirectory, "Semver.dll"));

            ModLoader.RunLoad("test.load-works", loader, () => { });

            Assert.That(ModFaults.IsFaulted("test.load-works"), Is.False);
        }

        [Test]
        public void MissingModsFolderIsCreated()
        {
            Directory.Delete(mods);

            loadAll();

            Assert.That(Directory.Exists(mods), Is.True);
            Assert.That(ModLoader.SKIPPED, Is.Empty);
            Assert.That(ModLoader.LOADED, Is.Empty);
        }

        [Test]
        public void FolderWithoutAManifestIsIgnored()
        {
            Directory.CreateDirectory(Path.Combine(mods, "loose-files"));

            loadAll();

            Assert.That(ModLoader.SKIPPED, Is.Empty);
        }

        [Test]
        public void ManifestThatBreaksTheSchemaSkipsTheMod()
        {
            writeMod("bad-id", "Not A Valid Id");

            loadAll();

            Assert.That(skipReason("bad-id"), Does.StartWith("mod.json is invalid: "));
        }

        [Test]
        public void ManifestThatIsntJsonSkipsTheMod()
        {
            string directory = Path.Combine(mods, "broken");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, ModLoader.MANIFEST_FILENAME), "{ \"id\": ");

            loadAll();

            Assert.That(skipReason("broken"), Does.StartWith("unreadable mod.json: "));
        }

        [Test]
        public void MissingEntryAssemblySkipsTheMod()
        {
            writeMod("no-entry", "test.no-entry", m => m["entry"] = "Missing.dll");

            loadAll();

            Assert.That(skipReason("no-entry"), Is.EqualTo("entry assembly Missing.dll not found"));
        }

        [Test]
        public void ModForAnIncompatibleApiIsSkipped()
        {
            writeMod("future", "test.future", m => m["apiVersion"] = "999.0");

            loadAll();

            Assert.That(skipReason("future"), Does.StartWith("built for API 999.0, "));
        }

        [Test]
        public void ModThatDependsOnItselfIsSkipped()
        {
            writeMod("loop", "test.loop", m => m["dependencies"] = requires("test.loop"));

            loadAll();

            Assert.That(skipReason("loop"), Is.EqualTo("it depends on itself"));
        }

        [Test]
        public void SecondModWithTheSameIdIsSkipped()
        {
            writeMod("first", "test.same");
            writeMod("second", "test.same");

            loadAll();

            Assert.That(skipReason("second in second"), Is.EqualTo("another mod with the same id is installed"));
            Assert.That(ModLoader.SKIPPED.Select(s => s.Name), Does.Not.Contain("first in first"));
        }

        [Test]
        public void ModTurnedOffInSettingsIsntLoadedOrSkipped()
        {
            writeMod("off", "test.off");
            var switches = openSwitches();
            switches.SetEnabled("test.off", false);

            loadAll(switches: switches);

            string[] expected = ["test.off"];
            Assert.That(ModLoader.TURNED_OFF.Select(c => c.Manifest.Id), Is.EqualTo(expected));
            Assert.That(ModLoader.SKIPPED, Is.Empty);
        }

        [Test]
        public void ModThatKeptCrashingIsSkipped()
        {
            writeMod("crashy", "test.crashy");
            var manifest = new ModManifest { Id = "test.crashy", Version = "1.0.0" };

            for (int session = 0; session < FaultHistory.STRIKES_TO_SKIP; session++)
                openHistory().Record(manifest, new InvalidOperationException("boom"));

            loadAll();

            Assert.That(skipReason("crashy"), Does.StartWith($"crashed in {FaultHistory.STRIKES_TO_SKIP} sessions in a row"));
        }

        [Test]
        public void MissingRequiredHookSkipsTheMod()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);
            writeMod("overlay", "test.overlay", m => m["hooks"] = new JsonObject { ["GlobalOverlay"] = "required" });

            loadAll();

            Assert.That(skipReason("overlay"), Is.EqualTo($"needs {FeatureRegistry.Info(Feature.GlobalOverlay).Name}, which this osu! version doesn't support"));
        }

        [Test]
        public void MissingOptionalHookDoesntSkipTheModForIt()
        {
            FeatureRegistry.Set(Feature.GlobalOverlay, FeatureState.Pending, null);
            writeMod("overlay", "test.overlay", m => m["hooks"] = new JsonObject { ["GlobalOverlay"] = "optional" });

            loadAll();

            Assert.That(skipReason("overlay"), Does.StartWith("failed to load: "));
        }

        [Test]
        public void DependingOnATurnedOffModSkipsTheMod()
        {
            writeMod("base", "test.base");
            writeMod("dependent", "test.dependent", m => m["dependencies"] = requires("test.base"));
            var switches = openSwitches();
            switches.SetEnabled("test.base", false);

            loadAll(switches: switches);

            Assert.That(skipReason("dependent"), Is.EqualTo("needs test.base, which is turned off"));
        }

        [Test]
        public void EntryThatIsntAnAssemblySkipsTheMod()
        {
            writeMod("garbage", "test.garbage");

            loadAll();

            Assert.That(skipReason("garbage"), Does.StartWith("failed to load: "));
            Assert.That(ModLoader.LOADED, Is.Empty);
        }

        [Test]
        public void DependingOnAModThatFailedToLoadSkipsTheMod()
        {
            writeMod("base", "test.base");
            writeMod("dependent", "test.dependent", m => m["dependencies"] = requires("test.base"));

            loadAll();

            Assert.That(skipReason("base"), Does.StartWith("failed to load: "));
            Assert.That(skipReason("dependent"), Is.EqualTo("needs test.base, which failed to load"));
        }

        [Test]
        public void EntryWithoutAModClassIsSkipped()
        {
            string directory = writeMod("library", "test.library", m => m["entry"] = "Semver.dll");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "Semver.dll"), Path.Combine(directory, "Semver.dll"));

            loadAll();

            Assert.That(skipReason("library"), Is.EqualTo($"expected exactly one {nameof(IMod)} implementation, found 0"));
        }

        [Test]
        public void WorkingModLoadsWithItsOwnContext()
        {
            string directory = installTestMod("working", "test.working");

            loadAll();

            Assert.That(ModLoader.SKIPPED, Is.Empty);
            var context = contextOf("test.working");
            Assert.That(context.Manifest.Id, Is.EqualTo("test.working"));
            Assert.That(context.ModDirectory, Is.EqualTo(directory));
        }

        [Test]
        public void RequiredDependencyIsLoadedFirstAndVisibleToTheMod()
        {
            installTestMod("base", "test.base");
            installTestMod("dependent", "test.dependent", m => m["dependencies"] = requires("test.base"));

            loadAll();

            string[] expected = ["test.base", "test.dependent"];
            Assert.That(ModLoader.LOADED.Select(m => m.Manifest.Id), Is.EqualTo(expected));
            Assert.That(contextOf("test.dependent").IsModLoaded("test.base"), Is.True);
        }

        [Test]
        public void MissingOptionalDependencyStillLoadsTheMod()
        {
            installTestMod("dependent", "test.dependent", m => m["dependencies"] = new JsonArray(new JsonObject { ["id"] = "not.installed", ["optional"] = true }));

            loadAll();

            Assert.That(ModLoader.IsLoaded("test.dependent"), Is.True);
            Assert.That(contextOf("test.dependent").IsModLoaded("not.installed"), Is.False);
        }

        [Test]
        public void TooOldOptionalDependencyStillLoadsTheModWithoutIt()
        {
            installTestMod("base", "test.base");
            installTestMod("dependent", "test.dependent",
                m => m["dependencies"] = new JsonArray(new JsonObject { ["id"] = "test.base", ["minVersion"] = "2.0", ["optional"] = true }));

            loadAll();

            Assert.That(ModLoader.IsLoaded("test.base"), Is.True);
            Assert.That(ModLoader.IsLoaded("test.dependent"), Is.True);
            Assert.That(contextOf("test.dependent").IsModLoaded("test.base"), Is.False);
        }

        private string installTestMod(string folder, string id, Action<JsonObject>? change = null)
        {
            string directory = writeMod(folder, id, m =>
            {
                m["entry"] = test_mod_assembly;
                change?.Invoke(m);
            });

            File.Copy(Path.Combine(AppContext.BaseDirectory, "testmod", test_mod_assembly), Path.Combine(directory, test_mod_assembly));
            return directory;
        }

        private static IModContext contextOf(string id)
        {
            var instance = ModLoader.Get(id)!.Instance;
            return (IModContext)instance.GetType().GetProperty("Context")!.GetValue(instance)!;
        }

        private const string test_mod_assembly = "Omamori.TestMod.dll";

        private string writeMod(string folder, string id, Action<JsonObject>? change = null)
        {
            string directory = Path.Combine(mods, folder);
            Directory.CreateDirectory(directory);

            var manifest = new JsonObject
            {
                ["id"] = id,
                ["name"] = folder,
                ["version"] = "1.0.0",
                ["entry"] = "Mod.dll",
                ["apiVersion"] = ApiInfo.VERSION,
                ["capabilities"] = new JsonArray("Cosmetic"),
            };

            change?.Invoke(manifest);

            File.WriteAllText(Path.Combine(directory, ModLoader.MANIFEST_FILENAME), manifest.ToJsonString());
            File.WriteAllText(Path.Combine(directory, "Mod.dll"), "not an assembly");
            return directory;
        }

        private static JsonArray requires(string id) => [new JsonObject { ["id"] = id }];

        private void loadAll(ModSwitches? switches = null) => ModLoader.LoadAll(mods, openHistory(), switches ?? openSwitches());

        private FaultHistory openHistory() => FaultHistory.Open(Path.Combine(root, FaultHistory.FILENAME));

        private ModSwitches openSwitches() => ModSwitches.Open(Path.Combine(root, ModSwitches.FILENAME));

        private static string skipReason(string name) => ModLoader.SKIPPED.Single(s => s.Name == name).Reason;
    }
}
