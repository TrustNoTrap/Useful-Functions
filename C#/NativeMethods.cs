﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

#nullable enable
namespace RunAsDomainUser
{
    class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessId;
            public Int32 dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct StartupInfo
        {
            public Int32 cb;
            public String lpReserved;
            public String lpDesktop;

            public String lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public uint dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public SafeFileHandle hStdInput;
            public SafeFileHandle hStdOutput;
            public SafeFileHandle hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SecurityAttributes
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;

            public SecurityAttributes()
            {
                this.Length = Marshal.SizeOf(this);
            }
        }

        [Flags]
        public enum LogonType
        {
            LOGON32_LOGON_INTERACTIVE = 2,
            LOGON32_LOGON_NETWORK = 3,
            LOGON32_LOGON_BATCH = 4,
            LOGON32_LOGON_SERVICE = 5,
            LOGON32_LOGON_UNLOCK = 7,
            LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
            LOGON32_LOGON_NEW_CREDENTIALS = 9
        }

        [Flags]
        public enum LogonProvider
        {
            LOGON32_PROVIDER_DEFAULT = 0,
            LOGON32_PROVIDER_WINNT35,
            LOGON32_PROVIDER_WINNT40,
            LOGON32_PROVIDER_WINNT50
        }

        [Flags]
        public enum CreateProcessFlags
        {
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_NO_WINDOW = 0x08000000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            DEBUG_PROCESS = 0x00000001,
            DETACHED_PROCESS = 0x00000008,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            INHERIT_PARENT_AFFINITY = 0x00010000
        }


        public class Constants
        {
            public const int HANDLE_FLAG_INHERIT = 1;
            public static uint STARTF_USESTDHANDLES = 0x00000100;
            public const UInt32 INFINITE = 0xFFFFFFFF;

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CreatePipe(out SafeFileHandle phReadPipe, out SafeFileHandle phWritePipe,
            SecurityAttributes lpPipeAttributes, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetHandleInformation(SafeFileHandle hObject, int dwMask, uint dwFlags);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Boolean LogonUser(
            String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            LogonType dwLogonType,
            LogonProvider dwLogonProvider,
            out SafeFileHandle phToken);

        [DllImport("userenv.dll", SetLastError = true)]
        public static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern Boolean CreateProcessAsUser
        (
            SafeFileHandle hToken,
            String? lpApplicationName,
            String lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            Boolean bInheritHandles,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            String? lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

         [Flags]
        public enum LogonFlags
        {
            LOGON_WITH_PROFILE = 1,
            LOGON_NETCREDENTIALS_ONLY = 2
        }


        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]

        public static extern bool CreateProcessWithLogonW(
            String userName,
            String? domain,
            String password,
            LogonFlags logonFlags,
            String? applicationName,
            String? commandLine,
            CreateProcessFlags creationFlags,
            IntPtr environment,
            String? currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);

        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(HandleRef hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
           string lpClassName,
           string lpWindowName,
           uint dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);
    }

    //static class WindowUtility
    //{
    //    [DllImport("user32.dll", SetLastError = true)]
    //    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    //    [DllImport("user32.dll", SetLastError = true)]
    //    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    //    const uint SWP_NOSIZE = 0x0001;
    //    const uint SWP_NOZORDER = 0x0004;

    //    private static Size GetScreenSize() => new Size(GetSystemMetrics(0), GetSystemMetrics(1));

    //    private struct Size
    //    {
    //        public int Width { get; set; }
    //        public int Height { get; set; }

    //        public Size(int width, int height)
    //        {
    //            Width = width;
    //            Height = height;
    //        }
    //    }

    //    [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
    //    private static extern int GetSystemMetrics(int nIndex);

    //    [DllImport("user32.dll")]
    //    [return: MarshalAs(UnmanagedType.Bool)]
    //    private static extern bool GetWindowRect(HandleRef hWnd, out Rect lpRect);

    //    [StructLayout(LayoutKind.Sequential)]
    //    private struct Rect
    //    {
    //        public int Left;        // x position of upper-left corner
    //        public int Top;         // y position of upper-left corner
    //        public int Right;       // x position of lower-right corner
    //        public int Bottom;      // y position of lower-right corner
    //    }

    //    private static Size GetWindowSize(IntPtr window)
    //    {
    //        if (!GetWindowRect(new HandleRef(null, window), out Rect rect))
    //            throw new Exception("Unable to get window rect!");

    //        int width = rect.Right - rect.Left;
    //        int height = rect.Bottom - rect.Top;

    //        return new Size(width, height);
    //    }

    //    public static void MoveWindowToCenter(IntPtr window) // Handle
    //    {
    //        if (window == IntPtr.Zero)
    //            throw new Exception("Couldn't find a window to center!");

    //        Size screenSize = GetScreenSize();
    //        Size windowSize = GetWindowSize(window);

    //        int x = (screenSize.Width - windowSize.Width) / 2;
    //        int y = (screenSize.Height - windowSize.Height) / 2;

    //        SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
    //    }
    //}
}   
