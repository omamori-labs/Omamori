using System;

namespace Omamori.Core
{
    /// <param name="Applied">methods patched before success or failure</param>
    /// <param name="RolledBack">patches removed again after a failure</param>
    /// <param name="Error">null when every class patched</param>
    internal sealed record ApplyResult(int Applied, int RolledBack, Exception? Error);
}
