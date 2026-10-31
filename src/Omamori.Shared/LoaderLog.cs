using System;
using osu.Framework.Logging;

namespace Omamori
{
    /// <summary>
    ///     writes to lazer's runtime log (logs/runtime.log) prefixed with [Omamori], only <see cref="Notify"/> reaches the player
    /// </summary>
    internal static class LoaderLog
    {
        public static void Info(string message) => Logger.Log($"[Omamori] {message}");

        // "important" shows up as ingame notification
        public static void Notify(string message) => Logger.Log($"[Omamori] {message}", level: LogLevel.Important);

        // not LogLevel.Error, lazer turns those into notifications too and these are for the log
        public static void Error(string message, Exception? exception = null)
        {
            Logger.Log($"[Omamori] error: {message}");

            if (exception != null)
                Logger.Log($"[Omamori] {exception}");
        }
    }
}
