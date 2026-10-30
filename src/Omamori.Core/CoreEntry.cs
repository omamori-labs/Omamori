using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Omamori.Core
{
    /// <summary>
    ///     called by the bootstrap through reflection
    /// </summary>
    public static class CoreEntry
    {
        public const int CONTRACT_VERSION = 1;

        public const string HARMONY_ID = "omamori.core";

        /// <summary>hidden by <see cref="Patches.HideHostRuleset"/></summary>
        internal static string HostRulesetShortName { get; private set; } = string.Empty;

        internal static string DataDirectory { get; private set; } = string.Empty;

        internal static string ModsDirectory => Path.Combine(DataDirectory, "omamori", "mods");

        private static readonly string[] shared_assemblies = ["Omamori.API", "0Harmony"];

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
            if (contractVersion != CONTRACT_VERSION)
                throw new InvalidOperationException($"bootstrap contract {contractVersion} doesn't match core contract {CONTRACT_VERSION}, update Omamori");

            DataDirectory = dataDirectory;
            HostRulesetShortName = hostRulesetShortName;

            AppDomain.CurrentDomain.AssemblyResolve += resolveSharedAssembly;

            LoaderLog.Info($"Omamori core {Version} starting (API {API.ApiInfo.VERSION}).");

            if (FaultInjection.Any)
                LoaderLog.Info($"Fault injection on: {Environment.GetEnvironmentVariable(FaultInjection.ENVIRONMENT_VARIABLE)}");

            FeatureRegistry.ApplyAll(new Harmony(HARMONY_ID));
        }

        private static Assembly? resolveSharedAssembly(object? sender, ResolveEventArgs args)
        {
            string? name = new AssemblyName(args.Name).Name;

            if (name == null || !shared_assemblies.Contains(name))
                return null;

            return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        }
    }
}
