using System.IO;
using osu.Framework.Platform;

namespace Omamori.Core.Manager
{
    internal static class ModsFolder
    {
        /// <summary>in the system's file browser, created first if it's missing</summary>
        public static void Open(GameHost host)
        {
            Directory.CreateDirectory(CoreEntry.ModsDirectory);
            host.OpenFileExternally(CoreEntry.ModsDirectory);
        }
    }
}
