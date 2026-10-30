using System;

namespace Omamori.Core.Safety
{
    internal sealed class InjectedFaultException(string message) : Exception(message);
}
