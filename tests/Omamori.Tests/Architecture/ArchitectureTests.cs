using System.Linq;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
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

        private static readonly System.Reflection.Assembly api = typeof(IMod).Assembly;
        private static readonly System.Reflection.Assembly core = typeof(CoreEntry).Assembly;
        private static readonly System.Reflection.Assembly bootstrap = typeof(OmamoriRuleset).Assembly;

        private const string any_core_namespace = @"^Omamori\.Core(\..+)?$";

        // the rules below only mean something if their predicates match real types
        [Test]
        public void ControlPatchesUseHarmony() =>
            Types().That().HaveFullNameMatching(@"^Omamori\.Core\.Patching\.Patches\+")
                   .Should().DependOnAny(Types().That().ResideInNamespace("HarmonyLib"))
                   .Check(architecture);

        // the loader's libraries stay in loading and safety
        [Test]
        public void LibrariesStayInLoadingAndSafety() =>
            Types().That().ResideInAssembly(core).And().DependOnAny(Types().That().ResideInNamespace("Semver")
                                                                            .Or().ResideInNamespace("QuikGraph")
                                                                            .Or().ResideInNamespace("McMaster.NETCore.Plugins"))
                   .Should().ResideInNamespaceMatching(@"^Omamori\.Core\.(Loading|Safety)$")
                   .Check(architecture);

        // mods compile against the api only, it can't leak core or its libraries
        [Test]
        public void ApiDependsOnNoOmamoriInternalsOrLibraries() =>
            Types().That().ResideInAssembly(api)
                   .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(any_core_namespace)
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
                   .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(any_core_namespace)
                                                   .Or().ResideInNamespace("Omamori.API")
                                                   .Or().ResideInNamespace("HarmonyLib"))
                   .Check(architecture);

        // harmony touches lazer internals, keep it to the shim, the registry that applies it and the one place that removes patches
        [Test]
        public void OnlyTheShimUsesHarmony() =>
            Types().That().ResideInAssembly(core).And().DependOnAny(Types().That().ResideInNamespace("HarmonyLib"))
                   .Should().HaveFullNameMatching(@"^Omamori\.Core\.(Patching\.Patches(\+.*)?|Features\.FeatureRegistry|CoreEntry|Loading\.ModIsolation|Safety\.(CrashAttribution|PatchRemoval))$")
                   .Check(architecture);

        [Test]
        public void ApiTypesAreAllPublicContracts() =>
            Types().That().ResideInAssembly(api).And().ArePublic()
                   .Should().ResideInNamespace("Omamori.API")
                   .Check(architecture);

        // patching is the top layer, it wires everything into lazer and nothing calls back into it
        [Test]
        public void NothingDependsOnPatching() =>
            coreOutside("Patching")
                .Should().NotDependOnAny(inArea("Patching"))
                .Check(architecture);

        // omamori's own ui sits on top of the rest, only patching puts it in the game
        [Test]
        public void OnlyPatchingUsesTheManager() =>
            coreOutside("Manager", "Patching")
                .Should().NotDependOnAny(inArea("Manager"))
                .Check(architecture);

        private static readonly string[] mod_services = ["Events", "Config", "ModUi", "Notifications", "Input", "Services"];

        // each is a separate service
        [TestCaseSource(nameof(mod_services))]
        public void ModServiceDependsOnNoOther(string service) =>
            inArea(service)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching($@"^Omamori\.Core\.({string.Join('|', mod_services.Where(s => s != service))})(\..+)?$"))
                .Check(architecture);

        private static string area(string name) => $@"^Omamori\.Core\.{name}(\..+)?$";

        private static GivenTypesConjunction inArea(string name) => Types().That().ResideInNamespaceMatching(area(name));

        private static GivenTypesConjunction coreOutside(params string[] names) =>
            Types().That().ResideInAssembly(core).And().DoNotResideInNamespaceMatching($@"^Omamori\.Core\.({string.Join('|', names)})(\..+)?$");
    }
}
