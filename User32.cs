using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using static GrassField.GFWindow;
using static GrassField.ProcessMetrics;

namespace GrassField
{
    internal static class User32
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr handle, out WindowRectangle lpRect);

        [DllImport("psapi.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, uint nSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern int CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr ProcessHandle, out IO_COUNTERS IoCounters);

        public static List<GFProcess> SetWindows(List<GFProcess> gfProcesses)
        {
            EnumWindows((hWnd, lParam) =>
            {
                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                var gf = gfProcesses.Find(x => x.ProcessID == processId);

                if (gf != null)
                {
                    gf.Windows.Add(new GFWindow(hWnd, processId));
                }
                return true;
            }, IntPtr.Zero);

            return gfProcesses;
        }

        public static string GetWindowTitle(GFWindow gfWindow)
        {
            var buffer = new System.Text.StringBuilder(256);
            if (GetWindowText(gfWindow.Handle, buffer, 256) > 0)
            {
                string windowText = buffer.ToString();
                return windowText;
            }
            return null;
        }

        public static WindowRectangle GetWindowRectangle(GFWindow gfWindow)
        {
            WindowRectangle windowRectangle;
            GetWindowRect(gfWindow.Handle, out windowRectangle);
            return windowRectangle;
        }

        public static bool IsWindowVisible(GFWindow gfWindow)
        {
            return IsWindowVisible(gfWindow.Handle);
        }

        public static string GetProcessPath(GFProcess gfProcess)
        {
            IntPtr processHandle = OpenProcess(0x0410, false, gfProcess.ProcessID);
            if (processHandle != IntPtr.Zero)
            {
                StringBuilder filePath = new StringBuilder(260); // Max path length
                GetModuleFileNameEx(processHandle, IntPtr.Zero, filePath, (uint)filePath.Capacity);
                CloseHandle(processHandle);

                Console.WriteLine("Chemin du fichier exécutable : " + filePath.ToString());
                return filePath.ToString();
            }

            return null;
        }

        internal static IO_COUNTERS GetProcessIOCounters(GFProcess gfProcess)
        {
            IO_COUNTERS ioC = new IO_COUNTERS();

            IntPtr processHandle = OpenProcess(0x0410, false, gfProcess.ProcessID);
            if (processHandle != IntPtr.Zero)
            {
                GetProcessIoCounters(processHandle, out ioC);
                return ioC;
            }

            return ioC;
        }

    }
}
