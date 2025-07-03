using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace InputMethodFix.Windows;

// WinImm32Ime interface not needed for WinImm32Ime implementation
internal class WindowsMessageHook : IDisposable
{
    private delegate IntPtr WndProcCallback(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int GWL_WNDPROC = -4;
    private IntPtr _windowHandle = IntPtr.Zero;
    private IntPtr _previousWndProc = IntPtr.Zero;
    private WndProcCallback _wndProc;
    private List<WinImm32Ime> _filters = new List<WinImm32Ime>();
    private bool disposedValue;

    public WindowsMessageHook(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        // Application.AddMessageFilter(this);
        _wndProc = WndProc;

        // SetWindowLong is for 32-bit applications, use SetWindowLongPtr for 64-bit
        // _previousWndProc = (IntPtr)Imm.SetWindowLong(_windowHandle, -4, (int)Marshal.GetFunctionPointerForDelegate((Delegate)_wndProc));
        _previousWndProc = NativeMethods.SetWindowLongPtr(_windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate((Delegate)_wndProc));
    }

    public void AddMessageFilter(WinImm32Ime filter)
    {
        _filters.Add(filter);
    }

    public void RemoveMessageFilter(WinImm32Ime filter)
    {
        _filters.Remove(filter);
    }

    private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        Message message = Message.Create(hWnd, msg, wParam, lParam);
        if (InternalWndProc(ref message))
            return message.Result;

        return NativeMethods.CallWindowProc(_previousWndProc, message.HWnd, message.Msg, message.WParam, message.LParam);
    }

    private bool InternalWndProc(ref Message message)
    {
        foreach (WinImm32Ime filter in _filters)
        {
            if (filter.PreFilterMessage(ref message))
                return true;
        }

        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            NativeMethods.SetWindowLongPtr(_windowHandle, GWL_WNDPROC, _previousWndProc);
            disposedValue = true;
        }
    }

    ~WindowsMessageHook()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
