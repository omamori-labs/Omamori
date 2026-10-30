using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Omamori.Build
{
    [SupportedOSPlatform("windows")]
    internal static class WindowCapture
    {
        private const uint wm_keydown = 0x0100;
        private const uint wm_keyup = 0x0101;
        private const int vk_escape = 0x1B;

        // PW_CLIENTONLY | PW_RENDERFULLCONTENT
        private const uint print_flags = 3;

        public static async Task CaptureAsync(Process game, string path)
        {
            game.Refresh();
            IntPtr window = game.MainWindowHandle;
            if (window == IntPtr.Zero)
                throw new InvalidOperationException("the game has no window to capture");

            postMessage(window, wm_keydown, vk_escape, IntPtr.Zero);
            postMessage(window, wm_keyup, vk_escape, IntPtr.Zero);
            await Task.Delay(TimeSpan.FromSeconds(3));

            getClientRect(window, out var rect);
            using var bitmap = new Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top);
            using var graphics = Graphics.FromImage(bitmap);

            IntPtr dc = graphics.GetHdc();
            bool ok = printWindow(window, dc, print_flags);
            graphics.ReleaseHdc(dc);

            if (!ok)
                throw new InvalidOperationException("PrintWindow failed");

            bitmap.Save(path);
        }

        [DllImport("user32.dll", EntryPoint = "PostMessageW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool postMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "PrintWindow")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool printWindow(IntPtr window, IntPtr dc, uint flags);

        [DllImport("user32.dll", EntryPoint = "GetClientRect")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool getClientRect(IntPtr window, out Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
