using System.Diagnostics.CodeAnalysis;
using Semver;

namespace Omamori.Core
{
    internal static class ModVersions
    {
        public const SemVersionStyles STYLES = SemVersionStyles.AllowWhitespace | SemVersionStyles.AllowV | SemVersionStyles.OptionalMinorPatch;

        public static bool TryParse(string? value, [NotNullWhen(true)] out SemVersion? version) => SemVersion.TryParse(value, STYLES, out version);
    }
}
