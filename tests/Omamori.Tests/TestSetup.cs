using System.IO;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace Omamori.Tests
{
    internal static class TestSetup
    {
        public static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(thisDirectory(), "..", ".."));

        [ModuleInitializer]
        internal static void Init()
        {
            DiffRunner.Disabled = true;

            VerifierSettings.DontIgnoreEmptyCollections();
        }

        private static string thisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
    }
}
