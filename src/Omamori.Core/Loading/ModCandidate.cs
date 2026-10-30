using System.IO;
using Omamori.API;

namespace Omamori.Core.Loading
{
    internal sealed record ModCandidate(string Directory, ModManifest Manifest)
    {
        public string Label => string.IsNullOrWhiteSpace(Manifest.Name) ? Path.GetFileName(Directory) : Manifest.Name;
    }
}
