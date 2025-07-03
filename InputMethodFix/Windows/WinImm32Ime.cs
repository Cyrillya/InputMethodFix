using System;
using System.Runtime.InteropServices;
using System.Text;

using InputMethodFix.Config;

namespace InputMethodFix.Windows;

[StructLayout(LayoutKind.Sequential)]
public struct Message
{
    public IntPtr HWnd;
    public int Msg;
    public IntPtr WParam;
    public IntPtr LParam;
    public IntPtr Result;

    public static Message Create(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam) => new Message()
    {
        HWnd = hWnd,
        Msg = msg,
        WParam = wparam,
        LParam = lparam,
        Result = IntPtr.Zero
    };
}

internal class WinImm32Ime : IImeService, IDisposable
{
    private readonly List<Action<char>> _keyPressCallbacks = new List<Action<char>>();
    private IntPtr _hWnd;
    private IntPtr _hImc;
    private bool _isFocused;
    private WindowsMessageHook _wndProcHook;
    private bool _disposedValue;
    private string _compString;
    private string[] _candList = Array.Empty<string>();
    private uint _candSelection;
    private uint _candPageSize;

    public uint SelectedPage => _candPageSize == 0 ? 0 : _candSelection / _candPageSize;

    public string CompositionString => _compString;

    public bool IsCandidateListVisible => CandidateCount > 0;

    public uint SelectedCandidate => _candSelection % _candPageSize;

    public uint CandidateCount => Math.Min((uint) _candList.Length - SelectedPage * _candPageSize, _candPageSize);

    public bool IsEnabled { get; private set; }

    public WinImm32Ime(WindowsMessageHook wndProcHook, IntPtr hWnd)
    {
        _wndProcHook = wndProcHook;
        _hWnd = hWnd;
        _hImc = NativeMethods.ImmGetContext(_hWnd);
        NativeMethods.ImmReleaseContext(_hWnd, _hImc);
        _isFocused = NativeMethods.GetForegroundWindow() == _hWnd;
        _wndProcHook.AddMessageFilter(this);
        SetEnabled(false);
    }

    public void SetEnabled(bool bEnable)
    {
        NativeMethods.ImmAssociateContext(_hWnd, bEnable ? _hImc : IntPtr.Zero);
    }

    public void FinalizeString(bool bSend = false)
    {
        IntPtr hImc = NativeMethods.ImmGetContext(_hWnd);
        try
        {
            NativeMethods.ImmNotifyIME(hImc, ImmConstants.NI_COMPOSITIONSTR, ImmConstants.CPS_CANCEL, 0);
            NativeMethods.ImmSetCompositionString(hImc, ImmConstants.SCS_SETSTR, "", 0, null, 0);
            NativeMethods.ImmNotifyIME(hImc, ImmConstants.NI_CLOSECANDIDATE, 0, 0);
        }
        finally
        {
            NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    public string GetCompositionString()
    {
        IntPtr hImc = NativeMethods.ImmGetContext(_hWnd);
        try
        {
            int size = NativeMethods.ImmGetCompositionString(hImc, ImmConstants.GCS_COMPSTR, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            if (size == 0)
            {
                return "";
            }

            Span<byte> buf = stackalloc byte[size];
            NativeMethods.ImmGetCompositionString(hImc, ImmConstants.GCS_COMPSTR, ref MemoryMarshal.GetReference(buf), size);

            return Encoding.Unicode.GetString(buf.ToArray());
        }
        finally
        {
            NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    public void UpdateCandidateList()
    {
        IntPtr hImc = NativeMethods.ImmGetContext(_hWnd);
        try
        {
            int size = NativeMethods.ImmGetCandidateList(hImc, 0, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            if (size == 0)
            {
                _candList = Array.Empty<string>();
                _candPageSize = 0;
                _candSelection = 0;
                return;
            }

            Span<byte> buf = stackalloc byte[size];
            NativeMethods.ImmGetCandidateList(hImc, 0, ref MemoryMarshal.GetReference(buf), size);

            ref CandidateList candList = ref MemoryMarshal.AsRef<CandidateList>(buf);
            var offsets = MemoryMarshal.CreateReadOnlySpan(ref candList.dwOffset, (int) candList.dwCount);

            string[] candStrList = new string[candList.dwCount];
            int next = buf.Length;
            for (int i = (int) candList.dwCount - 1; i >= 0; i--)
            {
                int start = (int) offsets[i];
                // Assume all strings are fully packed, with 2 byte null char at the end
                candStrList[i] = Encoding.Unicode.GetString(buf[start..(next - 2)]);
                next = start;
            }

            _candList = candStrList;
            _candPageSize = candList.dwPageSize;
            _candSelection = candList.dwSelection;
        }
        finally
        {
            NativeMethods.ImmReleaseContext(_hWnd, hImc);
        }
    }

    public string GetCandidate(uint index)
    {
        if (index < CandidateCount)
        {
            return _candList[index + SelectedPage * _candPageSize];
        }

        return "";
    }

    public void Enable()
    {
        if (!IsEnabled)
        {
            if (_isFocused)
                SetEnabled(bEnable: true);
            IsEnabled = true;
        }
    }

    public void Disable()
    {
        if (IsEnabled)
        {
            FinalizeString();
            SetEnabled(bEnable: false);
            IsEnabled = false;
        }
    }

    public void AddKeyListener(Action<char> listener)
    {
        _keyPressCallbacks.Add(listener);
    }

    public void RemoveKeyListener(Action<char> listener)
    {
        _keyPressCallbacks.Remove(listener);
    }

    protected void OnKeyPress(char character)
    {
        foreach (Action<char> keyPressCallback in _keyPressCallbacks)
        {
            keyPressCallback(character);
        }
    }

    public bool PreFilterMessage(ref Message message)
    {
        if (message.Msg == Msg.WM_KILLFOCUS)
        {
            SetEnabled(bEnable: false);
            _isFocused = false;
            return true;
        }

        if (message.Msg == Msg.WM_SETFOCUS)
        {
            if (IsEnabled)
                SetEnabled(bEnable: true);

            _isFocused = true;
            return true;
        }

        if (ModEntry.Config?.UseSystemIME != true)
        {
            // Hides the system IME. Should always be called on application startup.
            if (message.Msg == Msg.WM_IME_SETCONTEXT)
            {
                message.LParam = IntPtr.Zero;
                return false;
            }
        }

        if (!IsEnabled)
            return false;

        switch (message.Msg)
        {
            case Msg.WM_INPUTLANGCHANGE:
                return true;

            case Msg.WM_IME_STARTCOMPOSITION:
                _compString = "";
                return true;

            case Msg.WM_IME_COMPOSITION:
                _compString = GetCompositionString();
                break;

            case Msg.WM_IME_ENDCOMPOSITION:
                _compString = "";
                UpdateCandidateList();
                break;

            case Msg.WM_IME_NOTIFY:
                switch (message.WParam.ToInt32())
                {
                    case ImmConstants.IMN_OPENCANDIDATE:
                    case ImmConstants.IMN_CHANGECANDIDATE:
                    case ImmConstants.IMN_CLOSECANDIDATE:
                        UpdateCandidateList();
                        break;
                }

                return true;

            case Msg.WM_CHAR:
                OnKeyPress((char) message.WParam.ToInt32());
                break;
        }

        return false;
    }

    protected void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (IsEnabled)
                Disable();

            _wndProcHook.RemoveMessageFilter(this);
            NativeMethods.ImmAssociateContext(_hWnd, _hImc);
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~WinImm32Ime()
    {
        Dispose(disposing: false);
    }
}