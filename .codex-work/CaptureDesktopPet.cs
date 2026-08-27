using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

internal static class CaptureDesktopPet
{
    private delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumProc callback, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string windowName);

    private static int Main(string[] args)
    {
        if (args.Length != 2) return 2;
        uint pid = uint.Parse(args[0]);
        string path = args[1];
        IntPtr target = IntPtr.Zero;
        long bestArea = 0;
        IntPtr progman = FindWindow("Progman", null);

        if (pid == 0)
        {
            target = progman;
        }

        if (target == IntPtr.Zero) EnumChildWindows(progman, delegate(IntPtr hwnd, IntPtr unused)
        {
            uint windowPid;
            Rect rect;
            GetWindowThreadProcessId(hwnd, out windowPid);
            GetWindowRect(hwnd, out rect);
            long area = (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top);
            if (windowPid == pid && rect.Right - rect.Left > 500 && rect.Bottom - rect.Top > 500 && area > bestArea)
            {
                target = hwnd;
                bestArea = area;
            }
            return true;
        }, IntPtr.Zero);

        if (target == IntPtr.Zero && pid != 0) EnumWindows(delegate(IntPtr hwnd, IntPtr unused)
        {
            uint windowPid;
            Rect rect;
            GetWindowThreadProcessId(hwnd, out windowPid);
            GetWindowRect(hwnd, out rect);
            long area = (long)(rect.Right - rect.Left) * (rect.Bottom - rect.Top);
            if (windowPid == pid && rect.Right - rect.Left > 500 && rect.Bottom - rect.Top > 500 && area > bestArea)
            {
                target = hwnd;
                bestArea = area;
            }
            return true;
        }, IntPtr.Zero);

        if (target == IntPtr.Zero) return 3;
        Rect bounds;
        GetWindowRect(target, out bounds);
        using (Bitmap bitmap = new Bitmap(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdc = graphics.GetHdc();
            bool ok = PrintWindow(target, hdc, 2);
            graphics.ReleaseHdc(hdc);
            if (!ok) return 4;
            bitmap.Save(path, ImageFormat.Png);
        }
        return 0;
    }
}
