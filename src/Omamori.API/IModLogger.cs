using System;

namespace Omamori.API
{
    /// <summary>
    ///     writes to osu!'s runtime log with the mod's id, tell them with <see cref="IModContext.Notifications"/>
    /// </summary>
    public interface IModLogger
    {
        void Info(string message);

        /// <summary>log only, the exception's details go on the next line</summary>
        void Error(string message, Exception? exception = null);
    }
}
