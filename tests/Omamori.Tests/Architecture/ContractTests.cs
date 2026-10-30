using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Omamori.Bootstrap;
using Omamori.Core;

namespace Omamori.Tests
{
    [TestFixture]
    public class ContractTests
    {
        private static readonly MethodInfo? entry = typeof(CoreEntry).GetMethod(Layout.CORE_ENTRY_METHOD, BindingFlags.Public | BindingFlags.Static);

        [Test]
        public void EntryTypeIsCoreEntry() => Assert.That(typeof(CoreEntry).FullName, Is.EqualTo(Layout.CORE_ENTRY_TYPE));

        [Test]
        public void CoreAssemblyNameMatches() => Assert.That(typeof(CoreEntry).Assembly.GetName().Name, Is.EqualTo(Layout.CORE_ASSEMBLY));

        [Test]
        public void EntryMethodTakesContractDataDirectoryAndShortName()
        {
            System.Type[] expected = [typeof(int), typeof(string), typeof(string)];

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.GetParameters().Select(p => p.ParameterType), Is.EqualTo(expected));
        }

        // layout is compiled into both assemblies, a stale bootstrap build would fail
        [TestCase(nameof(Layout.CONTRACT_VERSION))]
        [TestCase(nameof(Layout.CORE_ASSEMBLY))]
        [TestCase(nameof(Layout.CORE_ENTRY_TYPE))]
        [TestCase(nameof(Layout.CORE_ENTRY_METHOD))]
        public void BootstrapCompiledTheSameLayout(string constant)
        {
            var bootstrapLayout = typeof(OmamoriRuleset).Assembly.GetType("Omamori.Layout", throwOnError: true)!;

            Assert.That(bootstrapLayout.GetField(constant)!.GetValue(null), Is.EqualTo(typeof(Layout).GetField(constant)!.GetValue(null)));
        }

        [Test]
        public void BootstrapAssemblyMatchesLazersRulesetPattern() =>
            Assert.That(typeof(OmamoriRuleset).Assembly.GetName().Name, Does.StartWith("osu.Game.Rulesets.").And.Not.Contain("Tests"));
    }
}
