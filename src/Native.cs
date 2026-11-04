using System;
using System.Runtime.InteropServices;

namespace FloatingTodo
{
    internal static class Native
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const uint HWND_BROADCAST = 0xFFFF;

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hwnd, IntPtr region, bool redraw);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int left, int top, int right, int bottom, int width, int height);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        public static bool ApplyGlass(IntPtr hwnd)
        {
            bool okay = false;
            int dark = 1;
            int darkLegacy = 1;
            int backdrop = 3;   // DWMSBT_TRANSIENTWINDOW (Acrylic)
            int corner = 1;     // DWMWCP_DONOTROUND: only the exact region shapes the window
            int borderColor = 0x0012100F;

            okay |= DwmSetWindowAttribute(hwnd, 20, ref dark, 4) == 0;
            DwmSetWindowAttribute(hwnd, 19, ref darkLegacy, 4);
            okay |= DwmSetWindowAttribute(hwnd, 38, ref backdrop, 4) == 0;
            DwmSetWindowAttribute(hwnd, 33, ref corner, 4);
            DwmSetWindowAttribute(hwnd, 34, ref borderColor, 4);

            MARGINS margins = new MARGINS();
            margins.Left = -1;
            margins.Right = -1;
            margins.Top = -1;
            margins.Bottom = -1;
            DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return okay;
        }

        public static void RemoveFromAltTab(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            style = (style | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, style);
        }

        public static double GetWindowScale(IntPtr hwnd)
        {
            try
            {
                uint dpi = GetDpiForWindow(hwnd);
                if (dpi > 0) return dpi / 96.0;
            }
            catch { }
            return 1.0;
        }

        public static void ApplyRoundedRegion(IntPtr hwnd, double widthDip, double heightDip, double radiusDip)
        {
            if (hwnd == IntPtr.Zero || widthDip <= 0 || heightDip <= 0) return;
            double scale = GetWindowScale(hwnd);
            int width = (int)Math.Round(widthDip * scale);
            int height = (int)Math.Round(heightDip * scale);
            int radius = (int)Math.Round(radiusDip * 2.0 * scale);
            if (width < 2 || height < 2) return;
            IntPtr region = CreateRoundRectRgn(0, 0, width + 1, height + 1, radius, radius);
            if (region == IntPtr.Zero) return;
            if (SetWindowRgn(hwnd, region, true) == 0)
            {
                DeleteObject(region);
            }
        }

        public static void Broadcast(uint message, bool quit)
        {
            PostMessage((IntPtr)HWND_BROADCAST, message, quit ? (IntPtr)2 : (IntPtr)1, IntPtr.Zero);
        }
    }
}
