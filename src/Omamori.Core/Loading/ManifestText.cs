using Omamori.API;

namespace Omamori.Core.Loading
{
    internal static class ManifestText
    {
        /// <summary>"by author", empty when the manifest names none</summary>
        public static string Byline(ModManifest manifest) => manifest.Author.Length > 0 ? $"by {manifest.Author}" : string.Empty;
    }
}
