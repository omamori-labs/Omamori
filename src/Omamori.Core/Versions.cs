using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Omamori.API;
using Semver;

namespace Omamori.Core
{
    internal static class ApiVersions
    {
        /// <summary>
        ///     <see cref="ApiInfo.VERSION"/>
        /// </summary>
        public static bool IsCompatible(string? modApiVersion, [NotNullWhen(false)] out string? problem)
        {
            var loader = parse(ApiInfo.VERSION)!.Value;

            if (parse(modApiVersion) is not { } mod)
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

        private static (int Major, int Minor)? parse(string? version)
        {
            string[] parts = (version ?? string.Empty).Trim().Split('.');

            if (parts.Length == 2 && parseNumber(parts[0], out int major) && parseNumber(parts[1], out int minor))
                return (major, minor);

            return null;
        }

        private static bool parseNumber(string text, out int number)
        {
            number = 0;

            if (text.Length == 0 || !text.All(char.IsAsciiDigit) || (text.Length > 1 && text[0] == '0'))
                return false;

            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
        }
    }

    internal static class ModVersions
    {
        public const SemVersionStyles STYLES = SemVersionStyles.AllowWhitespace | SemVersionStyles.AllowV | SemVersionStyles.OptionalMinorPatch;

        public static bool TryParse(string? value, [NotNullWhen(true)] out SemVersion? version) => SemVersion.TryParse(value, STYLES, out version);
    }
}
