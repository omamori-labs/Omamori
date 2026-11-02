using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Omamori.API;

namespace Omamori.NowPlaying
{
    internal sealed class StatusFile
    {
        private readonly IModContext context;
        private readonly object write_lock = new object();
        private int latest;
        private string? failedPath;

        public StatusFile(IModContext context)
        {
            this.context = context;
        }

        public void Write(string path, string text)
        {
            int mine = Interlocked.Increment(ref latest);

            _ = Task.Run(() =>
            {
                if (mine == Volatile.Read(ref latest))
                    WriteNow(path, text);
            });
        }

        public void WriteNow(string path, string text)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            lock (write_lock)
            {
                try
                {
                    if (!Path.IsPathFullyQualified(path))
                        throw new ArgumentException("needs a full path, like C:\\obs\\nowplaying.txt");

                    File.WriteAllText(path, text);
                    failedPath = null;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
                {
                    if (failedPath == path)
                        return;

                    failedPath = path;
                    context.Log.Error($"couldn't write {path}", e);
                    context.Notifications.PostError($"Couldn't write to {path}: {e.Message}");
                }
            }
        }
    }
}
