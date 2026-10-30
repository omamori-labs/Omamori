using System.Diagnostics.CodeAnalysis;
using Omamori.API;
using Semver;

namespace Omamori.Core
{
    internal static class ApiVersions
    {
        private const SemVersionStyles styles = SemVersionStyles.OptionalPatch;

        private static readonly SemVersion loader = SemVersion.Parse(ApiInfo.VERSION, styles);

        /// <summary>
        ///     <see cref="ApiInfo.VERSION"/>
        /// </summary>
        public static bool IsCompatible(string? modApiVersion, [NotNullWhen(false)] out string? problem)
        {
            if (!SemVersion.TryParse(modApiVersion, styles, out var mod))
            {
                problem = $"invalid apiVersion \"{modApiVersion}\", expected major.minor like \"{ApiInfo.VERSION}\"";
                return false;
            }

            if (mod.Major != loader.Major)
            {
                problem = $"built for API {modApiVersion}, Omamori provides {ApiInfo.VERSION} which isn't compatible";
                return false;
            }

            if (mod.Minor > loader.Minor)
            {
                problem = $"built for API {modApiVersion}, needs a newer Omamori (this one provides {ApiInfo.VERSION})";
                return false;
            }

            problem = null;
            return true;
        }
    }

    internal static class ModVersions
    {
        public const SemVersionStyles STYLES = SemVersionStyles.AllowWhitespace | SemVersionStyles.AllowV | SemVersionStyles.OptionalMinorPatch;

        public static bool TryParse(string? value, [NotNullWhen(true)] out SemVersion? version) => SemVersion.TryParse(value, STYLES, out version);
    }
}
