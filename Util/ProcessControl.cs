using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ComponentModel;
using System.Drawing;

namespace Fix_LCU_Window.Util
{
    internal class ProcessControl
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);

        [DllImport("user32.dll", EntryPoint = "GetDpiForWindow")]
        public static extern uint GetDpiForWindow([In] IntPtr hmonitor);

        [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll", EntryPoint = "GetWindowPlacement")]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [Serializable]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        public static string GetCommandLineByProcessId(uint processId)
        {
            try
            {
                return GetCommandLineArgsCore();
            }
            catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }

            string GetCommandLineArgsCore()
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
                using (var objects = searcher.Get())
                {
                    var @object = objects.Cast<ManagementBaseObject>().SingleOrDefault();
                    return @object?["CommandLine"]?.ToString() ?? "";
                }
            }
        }

        public static uint GetProcessId(String processName)
        {
            Process[] processes = Process.GetProcesses();
            uint iProcessId = 0;

            foreach (Process p in processes)
            {
                if (p.ProcessName == processName)
                {
                    iProcessId = (uint)p.Id;
                    break;
                }
            }

            return iProcessId;
        }

        public static bool IsMinimalized(IntPtr hWnd)
        {
            var placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            GetWindowPlacement(hWnd, ref placement);
            return placement.showCmd == 2; // SW_SHOWMINIMIZED 
        }
    }
}
