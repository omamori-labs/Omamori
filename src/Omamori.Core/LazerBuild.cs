using System.Linq;
using System.Reflection;
using osu.Game;

namespace Omamori.Core
{
    /// <summary>
    ///     which lazer build core was compiled against vs which one is running
    ///     a mismatch only warns
    /// </summary>
    internal static class LazerBuild
    {
        public static string BuiltFor { get; } =
            typeof(LazerBuild).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "LazerBuild")?.Value ?? string.Empty;

        public static string Running { get; } =
            Normalise(typeof(OsuGameBase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

        public static string? Mismatch(string builtFor, string running)
        {
            if (builtFor.Length == 0 || running.Length == 0 || builtFor == running)
                return null;

            return $"Omamori was built for osu! {builtFor} but this is {running}. It'll keep running, update Omamori if something breaks.";
        }

        public static string Normalise(string? informationalVersion) => (informationalVersion ?? string.Empty).Split('+')[0].Trim();

        public static void Check()
        {
            LoaderLog.Info($"Running on osu! {(Running.Length > 0 ? Running : "unknown")}, built for {(BuiltFor.Length > 0 ? BuiltFor : "unknown")}.");

            if (Mismatch(BuiltFor, Running) is string warning)
                LoaderLog.Notify(warning);
        }
    }
}
