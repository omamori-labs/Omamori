using System;
using System.IO;
using System.Reflection;
using HarmonyLib;

namespace Omamori.Core
{
    /// <summary>
    ///     called by the bootstrap through reflection
    /// </summary>
    public static class CoreEntry
    {
        public const string HARMONY_ID = "omamori.core";

        /// <summary>hidden by <see cref="Patches.HideHostRuleset"/></summary>
        internal static string HostRulesetShortName { get; private set; } = string.Empty;

        internal static string DataDirectory { get; private set; } = string.Empty;

        internal static string ModsDirectory => Layout.ModsDirectory(DataDirectory);

        /// <summary>
        ///     the real version from the git tag via minver, eg 0.1.0 or 0.1.0-alpha.0.5+sha
        /// </summary>
        internal static string Version { get; } =
            typeof(CoreEntry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        /// <summary>
        ///     runs inside lazers ruleset store ctor, so only patch here
        ///     mods load later, see <see cref="Patches.ModLoadingHook"/>
        /// </summary>
        public static void Start(int contractVersion, string dataDirectory, string hostRulesetShortName)
        {
            if (contractVersion != Layout.CONTRACT_VERSION)
                throw new InvalidOperationException($"bootstrap contract {contractVersion} doesn't match core contract {Layout.CONTRACT_VERSION}, update Omamori");

            DataDirectory = dataDirectory;
            HostRulesetShortName = hostRulesetShortName;

            LoaderLog.Info($"Omamori core {Version} starting (API {API.ApiInfo.VERSION}).");

            if (FaultInjection.Any)
                LoaderLog.Info($"Fault injection on: {Environment.GetEnvironmentVariable(FaultInjection.ENVIRONMENT_VARIABLE)}");

            LazerBuild.Check();

            FeatureRegistry.ApplyAll(new Harmony(HARMONY_ID));
        }
    }
}
