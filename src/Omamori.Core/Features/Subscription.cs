using System;
using System.Threading;

namespace Omamori.Core.Features
{
    /// <summary>
    ///     what a mod gets back for a handler, disposing it takes that one handler out, once, from any thread
    /// </summary>
    internal sealed class Subscription : IDisposable
    {
        /// <summary>for a handler that was never registered, like one for an unavailable hook</summary>
        public static readonly IDisposable NONE = new Subscription(null);

        private Action? remove;

        public Subscription(Action? remove)
        {
            this.remove = remove;
        }

        public void Dispose() => Interlocked.Exchange(ref remove, null)?.Invoke();
    }
}
