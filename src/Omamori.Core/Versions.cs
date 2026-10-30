using System.Diagnostics.CodeAnalysis;
using Semver;

namespace Omamori.Core
{
    internal static class ApiVersions
    {
        private const SemVersionStyles styles = SemVersionStyles.OptionalPatch;

        /// <summary>
        ///     whether a mod built for <paramref name="modApiVersion"/> runs on <paramref name="providedApiVersion"/>
        /// </summary>
        public static bool IsCompatible(string? modApiVersion, string providedApiVersion, [NotNullWhen(false)] out string? problem)
        {
            var provided = SemVersion.Parse(providedApiVersion, styles);

            if (!SemVersion.TryParse(modApiVersion, styles, out var mod))
            {
                problem = $"invalid apiVersion \"{modApiVersion}\", expected major.minor like \"{providedApiVersion}\"";
                return false;
            }

            if (mod.Major != provided.Major)
            {
                problem = $"built for API {modApiVersion}, Omamori provides {providedApiVersion} which isn't compatible";
                return false;
            }

            if (mod.Minor > provided.Minor)
            {
                problem = $"built for API {modApiVersion}, needs a newer Omamori (this one provides {providedApiVersion})";
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
