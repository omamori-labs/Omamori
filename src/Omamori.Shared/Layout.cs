using System.IO;

namespace Omamori
{
    /// <summary>
    ///     the contract between the bootstrap and core, both compile this file, so changing the entry means bumping <see cref="CONTRACT_VERSION"/>
    /// </summary>
    internal static class Layout
    {
        /// <summary>bump when <see cref="CORE_ENTRY_METHOD"/>'s signature changes</summary>
        public const int CONTRACT_VERSION = 1;

        public const string CORE_ASSEMBLY = "Omamori.Core";
        public const string CORE_ENTRY_TYPE = "Omamori.Core.CoreEntry";
        public const string CORE_ENTRY_METHOD = "Start";

        /// <summary>everything omamori keeps lives under [data]/omamori/</summary>
        public static string Root(string dataDirectory) => Path.Combine(dataDirectory, "omamori");

        public static string CoreDirectory(string dataDirectory) => Path.Combine(Root(dataDirectory), "core");

        public static string CoreAssembly(string dataDirectory) => Path.Combine(CoreDirectory(dataDirectory), $"{CORE_ASSEMBLY}.dll");

        public static string ModsDirectory(string dataDirectory) => Path.Combine(Root(dataDirectory), "mods");
    }
}
