using System;

namespace Omamori.Core
{
    internal sealed class InjectedFaultException(string message) : Exception(message);
}
