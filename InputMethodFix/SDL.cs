#region License
/* SDL2# - C# Wrapper for SDL2
 *
 * Copyright (c) 2013-2021 Ethan Lee.
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */
#endregion

/* 来自Cyrilly（Mod作者）的笔记：
 * 以下与调用SDL库有关的内容均直接复制于FNA库
 * 其实MonoGame也有SDL类，但是是internal的，而且也没找到Publicize的方法，不然我就直接用了
 */

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace InputMethodFix;

internal static class SDL
{
    // FIXME: I wish these weren't public...
    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_windows_wminfo
    {
        public IntPtr window; // Refers to an HWND
        public IntPtr hdc; // Refers to an HDC
        public IntPtr hinstance; // Refers to an HINSTANCE
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_winrt_wminfo
    {
        public IntPtr window; // Refers to an IInspectable*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_x11_wminfo
    {
        public IntPtr display; // Refers to a Display*
        public IntPtr window; // Refers to a Window (XID, use ToInt64!)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_directfb_wminfo
    {
        public IntPtr dfb; // Refers to an IDirectFB*
        public IntPtr window; // Refers to an IDirectFBWindow*
        public IntPtr surface; // Refers to an IDirectFBSurface*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_cocoa_wminfo
    {
        public IntPtr window; // Refers to an NSWindow*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_uikit_wminfo
    {
        public IntPtr window; // Refers to a UIWindow*
        public uint framebuffer;
        public uint colorbuffer;
        public uint resolveFramebuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_wayland_wminfo
    {
        public IntPtr display; // Refers to a wl_display*
        public IntPtr surface; // Refers to a wl_surface*
        public IntPtr shell_surface; // Refers to a wl_shell_surface*
        public IntPtr egl_window; // Refers to an egl_window*, requires >= 2.0.16
        public IntPtr xdg_surface; // Refers to an xdg_surface*, requires >= 2.0.16
        public IntPtr xdg_toplevel; // Referes to an xdg_toplevel*, requires >= 2.0.18
        public IntPtr xdg_popup;
        public IntPtr xdg_positioner;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_mir_wminfo
    {
        public IntPtr connection; // Refers to a MirConnection*
        public IntPtr surface; // Refers to a MirSurface*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_android_wminfo
    {
        public IntPtr window; // Refers to an ANativeWindow
        public IntPtr surface; // Refers to an EGLSurface
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_vivante_wminfo
    {
        public IntPtr display; // Refers to an EGLNativeDisplayType
        public IntPtr window; // Refers to an EGLNativeWindowType
    }

    /* Only available in 2.0.14 or higher. */
    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_os2_wminfo
    {
        public IntPtr hwnd; // Refers to an HWND
        public IntPtr hwndFrame; // Refers to an HWND
    }

    /* Only available in 2.0.16 or higher. */
    [StructLayout(LayoutKind.Sequential)]
    public struct INTERNAL_kmsdrm_wminfo
    {
        int dev_index;
        int drm_fd;
        IntPtr gbm_dev; // Refers to a gbm_device*
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INTERNAL_SysWMDriverUnion
    {
        [FieldOffset(0)]
        public INTERNAL_windows_wminfo win;
        [FieldOffset(0)]
        public INTERNAL_winrt_wminfo winrt;
        [FieldOffset(0)]
        public INTERNAL_x11_wminfo x11;
        [FieldOffset(0)]
        public INTERNAL_directfb_wminfo dfb;
        [FieldOffset(0)]
        public INTERNAL_cocoa_wminfo cocoa;
        [FieldOffset(0)]
        public INTERNAL_uikit_wminfo uikit;
        [FieldOffset(0)]
        public INTERNAL_wayland_wminfo wl;
        [FieldOffset(0)]
        public INTERNAL_mir_wminfo mir;
        [FieldOffset(0)]
        public INTERNAL_android_wminfo android;
        [FieldOffset(0)]
        public INTERNAL_os2_wminfo os2;
        [FieldOffset(0)]
        public INTERNAL_vivante_wminfo vivante;
        [FieldOffset(0)]
        public INTERNAL_kmsdrm_wminfo ksmdrm;
        // private int dummy;
    }

    public enum SDL_SYSWM_TYPE
    {
        SDL_SYSWM_UNKNOWN,
        SDL_SYSWM_WINDOWS,
        SDL_SYSWM_X11,
        SDL_SYSWM_DIRECTFB,
        SDL_SYSWM_COCOA,
        SDL_SYSWM_UIKIT,
        SDL_SYSWM_WAYLAND,
        SDL_SYSWM_MIR,
        SDL_SYSWM_WINRT,
        SDL_SYSWM_ANDROID,
        SDL_SYSWM_VIVANTE,
        SDL_SYSWM_OS2,
        SDL_SYSWM_HAIKU,
        SDL_SYSWM_KMSDRM /* requires >= 2.0.16 */
    }

    public enum SDL_bool
    {
        SDL_FALSE = 0,
        SDL_TRUE = 1
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_Rect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_SysWMinfo
    {
        public SDL_version version;
        public SDL_SYSWM_TYPE subsystem;
        public INTERNAL_SysWMDriverUnion info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_version
    {
        public byte major;
        public byte minor;
        public byte patch;
    }

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SDL_StartTextInput();

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void SDL_StopTextInput();

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern SDL_bool SDL_IsTextInputActive();

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_SetTextInputRect(ref SDL_Rect rect);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern SDL_bool SDL_GetWindowWMInfo(IntPtr window, ref SDL_SysWMinfo info);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_GetVersion(out SDL_version x);

    public static bool ParseToCSharpBool(SDL_bool SdlBool) => SdlBool switch
    {
        SDL_bool.SDL_FALSE => false,
        SDL_bool.SDL_TRUE => true,
        _ => throw new ArgumentNullException(),
    };

    public static bool IsTextInputActive() => ParseToCSharpBool(SDL_IsTextInputActive());

    public static void SetTextInputRect(Rectangle rect)
    {
        SDL_Rect SDL_Rect = new()
        {
            x = rect.X,
            y = rect.Y,
            w = rect.Width,
            h = rect.Height
        };
        SDL_SetTextInputRect(ref SDL_Rect);
    }

    /// <summary>
    /// Game1.game1.Window.Handle获取的是SDL窗口句柄，要转换为Win32窗口句柄
    /// </summary>
    /// <returns></returns>
    public static IntPtr GetWin32Handle(IntPtr windowHandle)
    {
        SDL_SysWMinfo info = new SDL_SysWMinfo();
        SDL_GetVersion(out info.version);
        SDL_GetWindowWMInfo(windowHandle, ref info);
        return info.info.win.window;
    }
}