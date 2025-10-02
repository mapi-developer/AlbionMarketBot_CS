using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public sealed class WindowCapture : IDisposable
{
    public enum CaptureMode
    {
        Auto,
        PrintWindow,
        BitBltWindow,
        BitBltClient
    }

    public enum TitleMatch
    {
        Exact,
        Contains
    }

    private IntPtr _hWnd;

    private WindowCapture(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) throw new ArgumentException("Invalid window handle.");
        _hWnd = hWnd;
    }

    public static void InitializeDpiAwareness()
    {
        try
        {
            if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                SetProcessDPIAware(); // legacy fallback
        }
        catch { /* ignore */ }
    }

    public IntPtr Handle => _hWnd;

    // ---------- Factory helpers ----------
    public static WindowCapture FromHandle(IntPtr hWnd) => new(hWnd);

    public static WindowCapture FromTitle(string title, TitleMatch matchMode = TitleMatch.Exact)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must be non-empty.");

        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, state) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            var text = GetWindowText(hWnd);
            if (string.IsNullOrEmpty(text)) return true;

            bool isMatch = matchMode == TitleMatch.Exact
                ? string.Equals(text, title, StringComparison.Ordinal)
                : text.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0;

            if (isMatch)
            {
                found = hWnd;
                return false; // stop
            }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
            throw new InvalidOperationException($"Window not found by title ({matchMode}): \"{title}\"");

        return new WindowCapture(found);
    }

    public static WindowCapture FromProcessId(int pid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, state) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            GetWindowThreadProcessId(hWnd, out uint wpid);
            if (wpid == (uint)pid)
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        if (found == IntPtr.Zero)
            throw new InvalidOperationException($"Top-level window not found for PID {pid}.");

        return new WindowCapture(found);
    }

    // ---------- Public capture API ----------
    public Bitmap Capture(CaptureMode mode = CaptureMode.Auto, bool includeFrame = true)
    {
        EnsureValid();

        if (IsIconic(_hWnd))
            throw new InvalidOperationException("Cannot capture a minimized window.");

        var bounds = includeFrame ? GetExtendedFrameBounds(_hWnd) : GetClientRectOnScreen(_hWnd);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException("Window has non-positive size.");

        return mode switch
        {
            CaptureMode.PrintWindow => CaptureViaPrintWindow(bounds, includeFrame),
            CaptureMode.BitBltWindow => CaptureViaBitBlt(bounds, clientArea: false),
            CaptureMode.BitBltClient => CaptureViaBitBlt(bounds, clientArea: true),
            CaptureMode.Auto => CaptureAuto(bounds, includeFrame),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }

    public byte[] CapturePng(CaptureMode mode = CaptureMode.Auto, bool includeFrame = true, long pngQualityIgnored = 100)
    {
        using var bmp = Capture(mode, includeFrame);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public void CaptureToStream(Stream stream, CaptureMode mode = CaptureMode.Auto, bool includeFrame = true, ImageFormat? format = null)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        using var bmp = Capture(mode, includeFrame);
        bmp.Save(stream, format ?? ImageFormat.Png);
    }

    public void Dispose() => _hWnd = IntPtr.Zero;

    // ---------- Internals ----------
    private Bitmap CaptureAuto(Rectangle bounds, bool includeFrame)
    {
        // 1) Try PrintWindow with full content (best for occluded windows, layered, DWM)
        if (TryCapturePrintWindow(bounds, includeFrame, out var bmp1)) return bmp1;

        // 2) Try BitBlt from whole window DC
        if (TryCaptureBitBlt(bounds, clientArea: false, out var bmp2)) return bmp2;

        // 3) Fallback to client area BitBlt
        if (TryCaptureBitBlt(bounds, clientArea: true, out var bmp3)) return bmp3;

        throw new InvalidOperationException("All capture paths failed (PrintWindow, BitBlt window, BitBlt client).");
    }

    private Bitmap CaptureViaPrintWindow(Rectangle bounds, bool includeFrame)
    {
        if (TryCapturePrintWindow(bounds, includeFrame, out var bmp)) return bmp;
        throw new InvalidOperationException("PrintWindow failed.");
    }

    private bool TryCapturePrintWindow(Rectangle bounds, bool includeFrame, out Bitmap bitmap)
    {
        bitmap = null!;
        using var screenDC = SafeDC.Screen();
        using var memDC = SafeDC.CreateCompatible(screenDC.Hdc);

        using var hBmp = SafeHBitmap.CreateCompatible(screenDC.Hdc, bounds.Width, bounds.Height);
        IntPtr old = SelectObject(memDC.Hdc, hBmp.Handle);
        try
        {
            uint flags = PW_RENDERFULLCONTENT;
            if (!includeFrame) flags |= PW_CLIENTONLY;

            bool ok = PrintWindow(_hWnd, memDC.Hdc, flags);
            if (!ok) return false;

            // Materialize a GDI+ Bitmap from HBITMAP
            bitmap = Image.FromHbitmap(hBmp.Handle);
            // Ensure alpha is preserved for layered windows: force 32bpp ARGB if needed
            if (Image.GetPixelFormatSize(bitmap.PixelFormat) != 32)
            {
                var clone = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(clone);
                g.DrawImageUnscaled(bitmap, 0, 0);
                bitmap.Dispose();
                bitmap = clone;
            }
            return true;
        }
        finally
        {
            SelectObject(memDC.Hdc, old);
        }
    }

    private Bitmap CaptureViaBitBlt(Rectangle bounds, bool clientArea)
    {
        if (TryCaptureBitBlt(bounds, clientArea, out var bmp)) return bmp;
        throw new InvalidOperationException(clientArea ? "BitBlt (client) failed." : "BitBlt (window) failed.");
    }

    private bool TryCaptureBitBlt(Rectangle bounds, bool clientArea, out Bitmap bitmap)
    {
        bitmap = null!;
        using var srcDC = clientArea ? SafeDC.Client(_hWnd) : SafeDC.Window(_hWnd);
        if (srcDC.Hdc == IntPtr.Zero) return false;

        using var screenDC = SafeDC.Screen();
        using var memDC = SafeDC.CreateCompatible(screenDC.Hdc);
        using var hBmp = SafeHBitmap.CreateCompatible(screenDC.Hdc, bounds.Width, bounds.Height);

        IntPtr old = SelectObject(memDC.Hdc, hBmp.Handle);
        try
        {
            // For window DC, coordinates are (0,0)
            // For client DC, also (0,0) because GetDC returns client-area origin
            bool ok = BitBlt(
                memDC.Hdc, 0, 0, bounds.Width, bounds.Height,
                srcDC.Hdc, 0, 0,
                SRCCOPY | CAPTUREBLT);

            if (!ok) return false;

            bitmap = Image.FromHbitmap(hBmp.Handle);
            if (Image.GetPixelFormatSize(bitmap.PixelFormat) != 32)
            {
                var clone = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(clone);
                g.DrawImageUnscaled(bitmap, 0, 0);
                bitmap.Dispose();
                bitmap = clone;
            }
            return true;
        }
        finally
        {
            SelectObject(memDC.Hdc, old);
        }
    }

    private static void EnsureValid()
    {
        // Note: this instance method uses _hWnd; do a quick global sanity check only
        // (per-instance validity is checked at the start of Capture)
    }

    private Rectangle GetExtendedFrameBounds(IntPtr hWnd)
    {
        if (!IsWindow(hWnd)) throw new ObjectDisposedException(nameof(WindowCapture), "Window handle is no longer valid.");

        // Prefer DWM extended frame bounds
        if (Environment.OSVersion.Version.Major >= 6 &&
            DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT ext, Marshal.SizeOf<RECT>()) == 0)
        {
            return Rectangle.FromLTRB(ext.Left, ext.Top, ext.Right, ext.Bottom);
        }

        if (!GetWindowRect(hWnd, out RECT r))
            throw new InvalidOperationException("GetWindowRect failed.");

        return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    private static Rectangle GetClientRectOnScreen(IntPtr hWnd)
    {
        if (!GetClientRect(hWnd, out RECT rc))
            throw new InvalidOperationException("GetClientRect failed.");

        var tl = new POINT { X = rc.Left, Y = rc.Top };
        if (!ClientToScreen(hWnd, ref tl))
            throw new InvalidOperationException("ClientToScreen failed.");

        return new Rectangle(tl.X, tl.Y, rc.Right - rc.Left, rc.Bottom - rc.Top);
    }

    private static string GetWindowText(IntPtr hWnd)
    {
        int len = GetWindowTextLength(hWnd);
        if (len <= 0) return string.Empty;
        var buf = new System.Text.StringBuilder(len + 1);
        _ = GetWindowText(hWnd, buf, buf.Capacity);
        return buf.ToString();
    }

    // ---------- Safe wrappers ----------
    private sealed class SafeDC : IDisposable
    {
        public IntPtr Hdc { get; private set; }
        private readonly IntPtr _hwnd;
        private readonly Source _src;

        private enum Source { Screen, Window, Client, Compatible }

        private SafeDC(IntPtr hdc, IntPtr hwnd, Source src)
        {
            Hdc = hdc;
            _hwnd = hwnd;
            _src = src;
        }

        public static SafeDC Screen() => new(GetDC(IntPtr.Zero), IntPtr.Zero, Source.Screen);
        public static SafeDC Window(IntPtr hwnd) => new(GetWindowDC(hwnd), hwnd, Source.Window);
        public static SafeDC Client(IntPtr hwnd) => new(GetDC(hwnd), hwnd, Source.Client);
        public static SafeDC CreateCompatible(IntPtr hdcRef) => new(CreateCompatibleDC(hdcRef), IntPtr.Zero, Source.Compatible);

        public void Dispose()
        {
            if (Hdc == IntPtr.Zero) return;

            switch (_src)
            {
                case Source.Screen:
                case Source.Client:
                    ReleaseDC(_hwnd, Hdc);
                    break;
                case Source.Window:
                    ReleaseDC(_hwnd, Hdc);
                    break;
                case Source.Compatible:
                    DeleteDC(Hdc);
                    break;
            }

            Hdc = IntPtr.Zero;
        }
    }

    private sealed class SafeHBitmap : IDisposable
    {
        public IntPtr Handle { get; private set; }
        private SafeHBitmap(IntPtr hBmp) => Handle = hBmp;
        public static SafeHBitmap CreateCompatible(IntPtr hdcRef, int w, int h)
        {
            var hBmp = CreateCompatibleBitmap(hdcRef, w, h);
            if (hBmp == IntPtr.Zero) throw new InvalidOperationException("CreateCompatibleBitmap failed.");
            return new SafeHBitmap(hBmp);
        }
        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                DeleteObject(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }

    // ---------- Win32 interop ----------
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int x, int y, int cx, int cy, IntPtr hdcSrc, int xSrc, int ySrc, int rop);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr hdc);        // <-- as above
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr hdc); // <-- as above

    private const int SRCCOPY = 0x00CC0020;
    private const int CAPTUREBLT = 0x40000000;


    private const uint PW_RENDERFULLCONTENT = 0x00000002;
    private const uint PW_CLIENTONLY = 0x00000001;

    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT pvAttribute, int cbAttribute);

    private enum DWMWINDOWATTRIBUTE { DWMWA_EXTENDED_FRAME_BOUNDS = 9 }

    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
}
