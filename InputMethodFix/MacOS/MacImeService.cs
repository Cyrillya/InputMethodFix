using System;

namespace InputMethodFix.MacOS;

internal class MacImeService : IImeService
{
    private readonly IntPtr _nsWindow;
    private nint _originalLevel;
    private static readonly IntPtr SelLevel = ObjCRuntime.sel_getUid("level");
    private static readonly IntPtr SelSetLevel = ObjCRuntime.sel_getUid("setLevel:");

    public MacImeService(IntPtr nsWindow)
    {
        _nsWindow = nsWindow;
    }

    public string CompositionString => "";
    public bool IsCandidateListVisible => false;
    public uint SelectedCandidate => 0;
    public uint CandidateCount => 0;
    public bool IsEnabled { get; private set; }

    public string GetCandidate(uint index) => "";

    public void Enable()
    {
        if (IsEnabled || _nsWindow == IntPtr.Zero)
            return;

        _originalLevel = (nint)ObjCRuntime.objc_msgSend(_nsWindow, SelLevel);
        ObjCRuntime.objc_msgSend(_nsWindow, SelSetLevel, 0); // NSNormalWindowLevel (0)

        IsEnabled = true;
    }

    public void Disable()
    {
        if (!IsEnabled || _nsWindow == IntPtr.Zero)
            return;

        ObjCRuntime.objc_msgSend(_nsWindow, SelSetLevel, _originalLevel);

        IsEnabled = false;
    }

    public void AddKeyListener(Action<char> listener) { }
    public void RemoveKeyListener(Action<char> listener) { }
}
