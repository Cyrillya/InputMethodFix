﻿using System.Runtime.InteropServices;

namespace InputMethodFix;

[StructLayout(LayoutKind.Sequential)]
public struct CandidateList
{
    public uint dwSize;
    public uint dwStyle;
    public uint dwCount;
    public uint dwSelection;
    public uint dwPageStart;
    public uint dwPageSize;
    public uint dwOffset;
}
