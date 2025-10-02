using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsInput;

public class InputSender
{
    [DllImport("user32.dll")]
    static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
    [DllImport("user32.dll")]
    static extern int SetForegroundWindow(IntPtr point);
    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    InputSimulator _simulator;
    private int _width;
    private int _height;
    private int _afterActionDelayMs = 100;

    public InputSender(int[] screenResolution)
    {
        _simulator = new InputSimulator();

        _width = screenResolution[0];
        _height = screenResolution[1];
    }

    public void SetForeground(string processName = "Albion-Online")
    {
        Process[] processes = Process.GetProcessesByName(processName);
        Process albionProcess = processes.FirstOrDefault();
        IntPtr hwnd = albionProcess.MainWindowHandle;
        SetForegroundWindow(hwnd);
        Thread.Sleep(_afterActionDelayMs);
    }

    public void MoveMouseTo(int[] position)
    {
        double[] bormalizedPosition = NormalizePosition(position);

        _simulator.Mouse.MoveMouseTo(bormalizedPosition[0], bormalizedPosition[1]);
        Thread.Sleep(_afterActionDelayMs);
    }

    public void ScrollMouse(int scrollingAmount)
    {
        _simulator.Mouse.VerticalScroll(scrollingAmount);
    }

    public void LeftClick(int[]? position)
    {
        if (position != null)
        {
            MoveMouseTo(position);
        }

        _simulator.Mouse.LeftButtonClick();
        Thread.Sleep(_afterActionDelayMs);
    }

    public void RightClick(int[]? position)
    {
        if (position != null)
        {
            MoveMouseTo(position);
        }

        _simulator.Mouse.RightButtonClick();
        Thread.Sleep(_afterActionDelayMs);
    }

    public void TypeText(string text, int delayMs = 25)
    {
        if (string.IsNullOrEmpty(text)) return;

        foreach (char ch in text)
        {
            _simulator.Keyboard.TextEntry(ch);
            if (delayMs > 0) Thread.Sleep(delayMs);
        }
        Thread.Sleep(_afterActionDelayMs);
    }

    public void KeyPress(VirtualKeyCode keyCode)
    {
        _simulator.Keyboard.KeyPress(keyCode);
        Thread.Sleep(_afterActionDelayMs * 2);
    }

    private double[] NormalizePosition(int[] position)
    {
        double normalizedX = (double)position[0] * 65535 / _width;
        double normalizedY = (double)position[1] * 65535 / _height;

        return [normalizedX, normalizedY];
    }
}