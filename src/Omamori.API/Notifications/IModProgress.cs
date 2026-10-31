using System.Threading;

namespace Omamori.API
{
    public interface IModProgress
    {
        /// <param name="progress">0 to 1</param>
        /// <param name="text">replaces the text when given</param>
        void Report(double progress, string? text = null);

        void Complete(string text);

        void Fail(string text);

        /// <summary>cancelled when the player cancels from the notification, or after <see cref="Fail"/></summary>
        CancellationToken Cancellation { get; }
    }
}
