using System;
using Omamori.API;
using osu.Framework.Logging;

namespace Omamori.Core
{
    /// <summary>
    ///     writes to lazer's runtime log (logs/runtime.log)  prefixed with [Omamori]
    /// </summary>
    internal static class LoaderLog
    {
        public static void Info(string message) => Logger.Log($"[Omamori] {message}");

        public static void Error(string message, Exception? exception = null)
        {
            Logger.Log($"[Omamori] {message}", level: LogLevel.Error);

            if (exception != null)
                Logger.Log($"[Omamori] {exception}");
        }
    }

    internal class ModLogger : IModLogger
    {
        private readonly string modId;

        public ModLogger(string modId)
        {
            this.modId = modId;
        }

        public void Info(string message) => LoaderLog.Info($"[{modId}] {message}");

        public void Error(string message, Exception? exception = null) => LoaderLog.Error($"[{modId}] {message}", exception);
    }
}
