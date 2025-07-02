using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;

using StardewValley;

namespace InputMethodFix;

internal static class CandidateHandler
{
    internal static ref IntPtr _hWnd => ref ModEntry.hWnd;
    internal static ref IntPtr _hImc => ref ModEntry.hImc;
    private static bool _disposedValue;
    private static string _compString;
    private static string[] _candList = Array.Empty<string>();
    private static uint _candSelection;
    private static uint _candPageSize;

    public static uint SelectedPage => _candPageSize == 0 ? 0 : _candSelection / _candPageSize;

    public static string CompositionString => _compString;

    public static bool IsCandidateListVisible => CandidateCount > 0;

    public static uint SelectedCandidate => _candSelection % _candPageSize;

    public static uint CandidateCount => Math.Min((uint) _candList.Length - SelectedPage * _candPageSize, _candPageSize);

    public static void SetState(bool state)
    {
        if (state)
        {
            _hImc = Imm.ImmGetContext(_hWnd);
            if (_hImc == IntPtr.Zero)
            {
                _hImc = Imm.ImmCreateContext();
                Imm.ImmAssociateContext(_hWnd, _hImc);
            }
            Imm.ImmReleaseContext(_hWnd, _hImc);
        }
        else
        {
            _hImc = Imm.ImmAssociateContext(_hWnd, IntPtr.Zero);
            if (_hImc != IntPtr.Zero)
            {
                Imm.ImmDestroyContext(_hImc);
            }
            Imm.ImmReleaseContext(_hWnd, _hImc);
        }
    }

    public static string GetCompositionString()
    {
        _hImc = Imm.ImmGetContext(_hWnd);
        if (_hImc == IntPtr.Zero)
        {
            _hImc = Imm.ImmCreateContext();
            Imm.ImmAssociateContext(_hWnd, _hImc);
        }
        try
        {
            int size = Imm.ImmGetCompositionString(_hImc, Imm.GCS_COMPSTR, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            if (size == 0)
            {
                return "";
            }

            Span<byte> buf = stackalloc byte[size];
            Imm.ImmGetCompositionString(_hImc, Imm.GCS_COMPSTR, ref MemoryMarshal.GetReference(buf), size);

            return Encoding.Unicode.GetString(buf.ToArray());
        }
        finally
        {
            Imm.ImmReleaseContext(_hWnd, _hImc);
        }
    }

    public static void UpdateCandidateList()
    {
        _hImc = Imm.ImmGetContext(_hWnd);
        if (_hImc == IntPtr.Zero)
        {
            _hImc = Imm.ImmCreateContext();
            Imm.ImmAssociateContext(_hWnd, _hImc);
        }
        try
        {
            int size = Imm.ImmGetCandidateList(_hImc, 0, ref MemoryMarshal.GetReference(Span<byte>.Empty), 0);
            Console.WriteLine($"候选数 {size}");
            if (size == 0)
            {
                _candList = Array.Empty<string>();
                _candPageSize = 0;
                _candSelection = 0;
                return;
            }

            Span<byte> buf = stackalloc byte[size];
            Imm.ImmGetCandidateList(_hImc, 0, ref MemoryMarshal.GetReference(buf), size);

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
            Imm.ImmReleaseContext(_hWnd, _hImc);
        }
    }

    public static string GetCandidate(uint index)
    {
        if (index < CandidateCount)
        {
            return _candList[index + SelectedPage * _candPageSize];
        }

        return "";
    }
}