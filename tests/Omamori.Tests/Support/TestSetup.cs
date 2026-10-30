using System;
using System.IO;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace Omamori.Tests
{
    internal static class TestSetup
    {
        public static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(thisDirectory(), "..", "..", ".."));

        [ModuleInitializer]
        internal static void Init()
        {
            DiffRunner.Disabled = true;

            VerifierSettings.DontIgnoreEmptyCollections();
        }

        internal static Exception Caught(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                return e;
            }

            throw new NUnit.Framework.AssertionException("expected a throw");
        }

        private static string thisDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;
    }
}
