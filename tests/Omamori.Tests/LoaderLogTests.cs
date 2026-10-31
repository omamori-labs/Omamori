using System;
using System.Collections.Concurrent;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Logging;

namespace Omamori.Tests
{
    /// <summary>
    ///     lazer shows any entry at <see cref="LogLevel.Important"/> or above to the player
    /// </summary>
    [TestFixture]
    public class LoaderLogTests
    {
        [Test]
        public void ErrorsStayInTheLog()
        {
            var entries = capture(marker => LoaderLog.Error($"broke {marker}", new InvalidOperationException($"boom {marker}")));

            Assert.That(entries, Has.Length.EqualTo(2));
            Assert.That(entries.Select(e => e.Level), Is.All.LessThan(LogLevel.Important));
        }

        [Test]
        public void NotifyReachesThePlayer() =>
            Assert.That(capture(marker => LoaderLog.Notify($"hello {marker}")).Single().Level, Is.EqualTo(LogLevel.Important));

        private static LogEntry[] capture(Action<string> log)
        {
            string marker = Guid.NewGuid().ToString("N");
            var entries = new ConcurrentQueue<LogEntry>();

            void collect(LogEntry entry)
            {
                if (entry.Message.Contains(marker, StringComparison.Ordinal))
                    entries.Enqueue(entry);
            }

            Logger.NewEntry += collect;

            try
            {
                log(marker);
            }
            finally
            {
                Logger.NewEntry -= collect;
            }

            return [.. entries];
        }
    }
}
