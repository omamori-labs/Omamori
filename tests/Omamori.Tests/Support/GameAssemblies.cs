using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Omamori.Tests
{
    internal static class GameAssemblies
    {
        public static string LazerPath { get; } =
            typeof(GameAssemblies).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "LazerPath").Value!;

        [ModuleInitializer]
        internal static void Register()
        {
            if (!File.Exists(Path.Combine(LazerPath, "osu.Game.dll")))
                throw new InvalidOperationException($"no lazer at {LazerPath}, set up sandbox/lazer first (see README)");

            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string path = Path.Combine(LazerPath, $"{name.Name}.dll");
                return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
            };
        }
    }
}
