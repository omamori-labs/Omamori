using System;

namespace Omamori.API
{
    public interface IModLogger
    {
        void Info(string message);
        void Error(string message, Exception? exception = null);
    }
}
