using System;
using System.IO;

namespace Omamori.Tests
{
    /// <summary>
    ///     every test folder is omamori-tests-[guid] under temp, delete it in TearDown
    /// </summary>
    internal static class TempDirectory
    {
        /// <summary>only the path, for tests where the code under test has to create it</summary>
        public static string New() => Path.Combine(Path.GetTempPath(), $"omamori-tests-{Guid.NewGuid()}");

        public static string Create()
        {
            string path = New();
            Directory.CreateDirectory(path);
            return path;
        }

        public static void Delete(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
    }
}
