using System;
using Omamori.API;

namespace Omamori.Core.Loading
{
    internal sealed class ModLogger : IModLogger
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
