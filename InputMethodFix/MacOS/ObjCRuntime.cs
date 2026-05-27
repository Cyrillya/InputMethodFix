using System.Runtime.InteropServices;

namespace InputMethodFix.MacOS;

internal static class ObjCRuntime
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLib)]
    internal static extern IntPtr sel_getUid(string name);

    [DllImport(ObjCLib)]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib)]
    internal static extern void objc_msgSend(IntPtr receiver, IntPtr selector, nint arg);
}
