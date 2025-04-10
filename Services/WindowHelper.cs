using System.Runtime.InteropServices;
using System.Text;

namespace BackEnd_WebSocket.Services
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string? Title { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static class ScreenHelper
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        const int HORZRES = 8;
        const int VERTRES = 10;

        public static (int Width, int Height) GetScreenResolution()
        {
            IntPtr hDC = GetDC(IntPtr.Zero);
            int width = GetDeviceCaps(hDC, HORZRES);
            int height = GetDeviceCaps(hDC, VERTRES);
            ReleaseDC(IntPtr.Zero, hDC);
            return (width, height);
        }
    }


    public static class WindowHelper
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        private const uint WM_CLOSE = 0x0010;

        public static void CerrarVentana(IntPtr hWnd)
        {
            PostMessage(hWnd, WM_CLOSE, 0, 0);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
);

        private const uint SWP_NOSIZE = 0x0001;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        public static void MoverVentana(IntPtr hWnd, int x, int y)
        {
            SetWindowPos(hWnd, HWND_TOP, x, y, 0, 0, SWP_NOSIZE);
        }


        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static List<WindowInfo> GetOpenWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();

                if (string.IsNullOrWhiteSpace(title))
                    return true;

                GetWindowRect(hWnd, out RECT rect);

                windows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    X = rect.Left,
                    Y = rect.Top,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }
    }
}
