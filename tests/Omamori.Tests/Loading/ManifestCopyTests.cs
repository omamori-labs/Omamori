using System;
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
            Capabilities = [ModCapability.Patches],
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
        public void CopysCollectionsCantBeCastBackAndChanged()
        {
            var original = manifest();
            var copy = ManifestCopy.Of(original);

            Assert.Throws<NotSupportedException>(() => ((IList<ModCapability>)copy.Capabilities).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<Hook, HookRequirement>)copy.Hooks)[Hook.Input] = HookRequirement.Required);
            Assert.Throws<NotSupportedException>(() => ((IList<ModDependency>)copy.Dependencies).Clear());
            Assert.That(JsonSerializer.Serialize(original), Is.EqualTo(JsonSerializer.Serialize(manifest())));
        }

        [Test]
        public void CopySharesNothingWithTheOriginal()
        {
            var original = manifest();
            var copy = ManifestCopy.Of(original);

            Assert.That(copy.Capabilities, Is.Not.SameAs(original.Capabilities));
            Assert.That(copy.Hooks, Is.Not.SameAs(original.Hooks));
            Assert.That(copy.Dependencies[0], Is.Not.SameAs(original.Dependencies[0]));
        }
    }
}
