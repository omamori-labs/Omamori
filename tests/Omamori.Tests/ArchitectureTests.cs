using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using Omamori.API;
using Omamori.Bootstrap;
using Omamori.Core;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Omamori.Tests
{
    [TestFixture]
    public class ArchitectureTests
    {
        private static readonly Architecture architecture = new ArchLoader()
                                                            .LoadAssemblies(
                                                                typeof(IMod).Assembly,
                                                                typeof(CoreEntry).Assembly,
                                                                typeof(OmamoriRuleset).Assembly,
                                                                typeof(HarmonyLib.Harmony).Assembly,
                                                                typeof(McMaster.NETCore.Plugins.PluginLoader).Assembly,
                                                                typeof(Semver.SemVersion).Assembly,
                                                                typeof(QuikGraph.AdjacencyGraph<,>).Assembly,
                                                                typeof(Json.Schema.JsonSchema).Assembly)
                                                            .Build();

        [Test]
        public void ControlPatchesUseHarmony() =>
            Types().That().HaveFullNameMatching(@"^Omamori\.Core\.Patches\+")
                   .Should().DependOnAny(Types().That().ResideInNamespace("HarmonyLib"))
                   .Check(architecture);

        [Test]
        public void ControlCoreUsesItsLibraries() =>
            Types().That().ResideInAssembly(typeof(CoreEntry).Assembly).And().DependOnAny(Types().That().ResideInNamespace("Semver")
                                                                                                      .Or().ResideInNamespace("QuikGraph")
                                                                                                      .Or().ResideInNamespace("McMaster.NETCore.Plugins"))
                   .Should().ResideInNamespace("Omamori.Core")
                   .Check(architecture);

        private static readonly System.Reflection.Assembly api = typeof(IMod).Assembly;
        private static readonly System.Reflection.Assembly core = typeof(CoreEntry).Assembly;
        private static readonly System.Reflection.Assembly bootstrap = typeof(OmamoriRuleset).Assembly;

        // mods compile against the api only, it can't leak core or its libraries
        [Test]
        public void ApiDependsOnNoOmamoriInternalsOrLibraries() =>
            Types().That().ResideInAssembly(api)
                   .Should().NotDependOnAny(Types().That().ResideInNamespace("Omamori.Core")
                                                   .Or().ResideInNamespace("HarmonyLib")
                                                   .Or().ResideInNamespace("McMaster.NETCore.Plugins")
                                                   .Or().ResideInNamespace("Semver")
                                                   .Or().ResideInNamespace("QuikGraph")
                                                   .Or().ResideInNamespace("Json.Schema"))
                   .Check(architecture);

        // the bootstrap sits in lazer's rulesets folder alone, it can only use the game
        [Test]
        public void BootstrapDependsOnlyOnTheGame() =>
            Types().That().ResideInAssembly(bootstrap)
                   .Should().NotDependOnAny(Types().That().ResideInNamespace("Omamori.Core")
                                                   .Or().ResideInNamespace("Omamori.API")
                                                   .Or().ResideInNamespace("HarmonyLib"))
                   .Check(architecture);

        // harmony touches lazer internals, keep it to the shim and the registry that applies it
        [Test]
        public void OnlyTheShimUsesHarmony() =>
            Types().That().ResideInAssembly(core).And().DependOnAny(Types().That().ResideInNamespace("HarmonyLib"))
                   .Should().HaveFullNameMatching(@"^Omamori\.Core\.(Patches(\+.*)?|FeatureRegistry|CoreEntry|ModIsolation|CrashAttribution)$")
                   .Check(architecture);

        [Test]
        public void ApiTypesAreAllPublicContracts() =>
            Types().That().ResideInAssembly(api).And().ArePublic()
                   .Should().ResideInNamespace("Omamori.API")
                   .Check(architecture);
    }
}
