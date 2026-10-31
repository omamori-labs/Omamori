using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using Omamori.API;
using Omamori.Core.Loading;

namespace Omamori.Tests
{
    [TestFixture]
    public class ManifestCopyTests
    {
        private static ModManifest manifest() => new ModManifest
        {
            Id = "test.copy",
            Name = "Copy",
            Version = "1.0.0",
            Author = "someone",
            Description = "a mod",
            Entry = "Copy.dll",
            ApiVersion = ApiInfo.VERSION,
            Capabilities = [ModCapability.Cosmetic],
            Hooks = new Dictionary<Hook, HookRequirement> { [Hook.MainMenu] = HookRequirement.Required },
            Dependencies = [new ModDependency { Id = "test.dep", MinVersion = "1.0.0" }],
        };

        [Test]
        public void CopiesEverything()
        {
            var original = manifest();

            Assert.That(JsonSerializer.Serialize(ManifestCopy.Of(original)), Is.EqualTo(JsonSerializer.Serialize(original)));
        }

        [Test]
        public void ChangingTheCopyLeavesTheOriginal()
        {
            var original = manifest();
            var copy = ManifestCopy.Of(original);

            copy.Id = "other.mod";
            copy.Capabilities.Add(ModCapability.Informational);
            copy.Hooks[Hook.KeyBindings] = HookRequirement.Required;
            copy.Dependencies[0].Optional = true;
            copy.Dependencies.Clear();

            Assert.That(JsonSerializer.Serialize(original), Is.EqualTo(JsonSerializer.Serialize(manifest())));
        }
    }
}
