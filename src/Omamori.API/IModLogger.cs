using System;

namespace Omamori.API
{
    /// <summary>
    ///     writes to osu!'s runtime log with the mod's id, safe from any thread
    ///     the player rarely reads the log, tell them with <see cref="IModContext.Notifications"/>
    /// </summary>
    public interface IModLogger
    {
        void Info(string message);

        /// <summary>something went wrong but the mod carries on, the exception's details go on the next line</summary>
        void Warning(string message, Exception? exception = null);

        /// <summary>log only like the rest, it never becomes a notification, the exception's details go on the next line</summary>
        void Error(string message, Exception? exception = null);
    }
}
