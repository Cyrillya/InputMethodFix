using System.Runtime.InteropServices;

namespace InputMethodFix.Windows;

/// <summary>
/// Reads candidate lists exposed through TSF UI elements.  Weasel publishes its
/// candidates here instead of populating the IMM32 CANDIDATELIST structure.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class TsfCandidateSink : ITfUIElementSink, IDisposable
{
    // Returned by CCandidateList::GetGUID in WeaselTSF/CandidateList.cpp.
    private static readonly Guid WeaselCandidateElementGuid =
        new("36c3c795-7159-45aa-ab12-30229a51dbd3");

    private static readonly Guid UIElementSinkGuid =
        new("ea1ea136-19df-11d7-a6d2-00065b84435c");

    private readonly object _candidateLock = new();
    private ITfUIElementMgr? _uiElementManager;
    private ITfSource? _source;
    private uint _sinkCookie = uint.MaxValue;
    private uint _weaselElementId = uint.MaxValue;
    private string[] _candidates = Array.Empty<string>();
    private uint _selection;
    private bool _disposed;

    public bool IsInitialized => _source != null;

    /// <summary>Attach to the TSF manager already owned by SDL on this thread.</summary>
    public bool TryInitialize()
    {
        if (!OperatingSystem.IsWindows() || _disposed || IsInitialized)
            return IsInitialized;

        IntPtr threadManagerPointer = IntPtr.Zero;
        try
        {
            int result = TF_GetThreadMgr(out threadManagerPointer);
            if (result < 0 || threadManagerPointer == IntPtr.Zero)
                return false;

            object threadManager = Marshal.GetObjectForIUnknown(threadManagerPointer);
            _uiElementManager = (ITfUIElementMgr)threadManager;
            _source = (ITfSource)threadManager;

            Guid sinkGuid = UIElementSinkGuid;
            result = _source.AdviseSink(ref sinkGuid, this, out _sinkCookie);
            if (result < 0)
            {
                _source = null;
                _uiElementManager = null;
                _sinkCookie = uint.MaxValue;
                return false;
            }

            return true;
        }
        catch (COMException)
        {
            _source = null;
            _uiElementManager = null;
            _sinkCookie = uint.MaxValue;
            return false;
        }
        finally
        {
            // Balance the reference returned by TF_GetThreadMgr. The RCW keeps
            // its own reference while the sink is active.
            if (threadManagerPointer != IntPtr.Zero)
                Marshal.Release(threadManagerPointer);
        }
    }

    public (string[] Candidates, uint Selection) GetSnapshot()
    {
        lock (_candidateLock)
            return ((string[])_candidates.Clone(), _selection);
    }

    public void ResetCandidates()
    {
        _weaselElementId = uint.MaxValue;
        ClearCandidates();
    }

    public int BeginUIElement(uint elementId, ref int show)
    {
        try
        {
            if (!IsWeaselCandidateElement(elementId))
                return 0;

            _weaselElementId = elementId;
            ReadCandidateList(elementId);

            // Match the existing IMM32 behavior: the native panel is retained
            // only when the user explicitly selected "Use System IME".
            show = ModEntry.Config?.UseSystemIME == true ? 1 : 0;
        }
        catch
        {
            // Exceptions must never escape a COM callback into msctf.dll.
        }

        return 0;
    }

    public int UpdateUIElement(uint elementId)
    {
        try
        {
            if (elementId == _weaselElementId || IsWeaselCandidateElement(elementId))
            {
                _weaselElementId = elementId;
                ReadCandidateList(elementId);
            }
        }
        catch
        {
            // Exceptions must never escape a COM callback into msctf.dll.
        }

        return 0;
    }

    public int EndUIElement(uint elementId)
    {
        if (elementId == _weaselElementId)
        {
            ResetCandidates();
        }

        return 0;
    }

    private bool IsWeaselCandidateElement(uint elementId)
    {
        if (_uiElementManager == null ||
            _uiElementManager.GetUIElement(elementId, out ITfUIElement? element) < 0 ||
            element == null)
        {
            return false;
        }

        return element.GetGuid(out Guid elementGuid) >= 0 &&
               elementGuid == WeaselCandidateElementGuid &&
               element is ITfCandidateListUIElement;
    }

    private void ReadCandidateList(uint elementId)
    {
        if (_uiElementManager == null ||
            _uiElementManager.GetUIElement(elementId, out ITfUIElement? element) < 0 ||
            element is not ITfCandidateListUIElement candidateList ||
            candidateList.GetCount(out uint count) < 0 ||
            candidateList.GetSelection(out uint selection) < 0)
        {
            return;
        }

        // A normal IME page is small. This cap also protects the game from a
        // malformed third-party UI element reporting an unreasonable count.
        count = Math.Min(count, 100u);
        string[] candidates = new string[count];
        for (uint i = 0; i < count; i++)
        {
            IntPtr candidateString = IntPtr.Zero;
            try
            {
                if (candidateList.GetString(i, out candidateString) >= 0 &&
                    candidateString != IntPtr.Zero)
                {
                    // Weasel currently allocates its BSTR with one trailing NUL.
                    candidates[i] = (Marshal.PtrToStringBSTR(candidateString) ?? "")
                        .TrimEnd('\0');
                }
            }
            finally
            {
                if (candidateString != IntPtr.Zero)
                    Marshal.FreeBSTR(candidateString);
            }
        }

        lock (_candidateLock)
        {
            _candidates = candidates;
            _selection = selection < count ? selection : 0;
        }
    }

    private void ClearCandidates()
    {
        lock (_candidateLock)
        {
            _candidates = Array.Empty<string>();
            _selection = 0;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_source != null && _sinkCookie != uint.MaxValue)
        {
            try
            {
                _source.UnadviseSink(_sinkCookie);
            }
            catch (COMException)
            {
                // TSF may already be shutting down with the game process.
            }
        }

        ResetCandidates();
        _source = null;
        _uiElementManager = null;
        _sinkCookie = uint.MaxValue;
    }

    [DllImport("msctf.dll", ExactSpelling = true)]
    private static extern int TF_GetThreadMgr(out IntPtr threadManager);
}

[ComVisible(true)]
[Guid("ea1ea136-19df-11d7-a6d2-00065b84435c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ITfUIElementSink
{
    [PreserveSig]
    int BeginUIElement(uint elementId, ref int show);

    [PreserveSig]
    int UpdateUIElement(uint elementId);

    [PreserveSig]
    int EndUIElement(uint elementId);
}

[ComImport]
[Guid("4ea48a35-60ae-446f-8fd6-e6a8d82459f7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfSource
{
    [PreserveSig]
    int AdviseSink(
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.IUnknown)] object sink,
        out uint cookie);

    [PreserveSig]
    int UnadviseSink(uint cookie);
}

[ComImport]
[Guid("ea1ea135-19df-11d7-a6d2-00065b84435c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfUIElementMgr
{
    [PreserveSig]
    int BeginUIElement(ITfUIElement element, ref int show, out uint elementId);

    [PreserveSig]
    int UpdateUIElement(uint elementId);

    [PreserveSig]
    int EndUIElement(uint elementId);

    [PreserveSig]
    int GetUIElement(uint elementId, out ITfUIElement? element);

    [PreserveSig]
    int EnumUIElements(out IntPtr enumerator);
}

[ComImport]
[Guid("ea1ea137-19df-11d7-a6d2-00065b84435c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfUIElement
{
    [PreserveSig]
    int GetDescription(out IntPtr description);

    [PreserveSig]
    int GetGuid(out Guid guid);

    [PreserveSig]
    int Show(int show);

    [PreserveSig]
    int IsShown(out int show);
}

[ComImport]
[Guid("ea1ea138-19df-11d7-a6d2-00065b84435c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfCandidateListUIElement : ITfUIElement
{
    [PreserveSig]
    int GetUpdatedFlags(out uint flags);

    [PreserveSig]
    int GetDocumentMgr(out IntPtr documentManager);

    [PreserveSig]
    int GetCount(out uint count);

    [PreserveSig]
    int GetSelection(out uint index);

    [PreserveSig]
    int GetString(uint index, out IntPtr value);

    [PreserveSig]
    int GetPageIndex(IntPtr pageIndexes, uint size, out uint pageCount);

    [PreserveSig]
    int SetPageIndex(IntPtr pageIndexes, uint pageCount);

    [PreserveSig]
    int GetCurrentPage(out uint currentPage);
}
