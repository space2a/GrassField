using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;

namespace GrassField
{

    public sealed class GFService : GFProcess
    {
        internal GFService(ManagementBaseObject managementBaseObject) : base(managementBaseObject) { }
    }

    public class GFUnknown : GFProcess
    {
        internal GFUnknown(uint pid) : base(pid) { }
    }

    public class GFProcess
    {
        //TODO : MainWindowTitle, MainWindowHandle | Metrics
        public readonly GFProcess ParentProcess;

        public List<GFProcess> SubProcesses = new List<GFProcess>();

        public GFWindow MainWindow => GetMainWindow();
        public List<GFWindow> Windows = new List<GFWindow>();

        public bool IsVisible => IsProcessVisible();

        public readonly uint ProcessID;

        public readonly string ProcessName;

        private readonly string _executablePath = null;
        public string ExecutablePath
        {
            get { if (_executablePath == null) return User32.GetProcessPath(this); else return _executablePath; }
        }

        public readonly string CommandLine;

        public readonly string ProcessDescription;

        public bool IsService { get { return (this is GFService); } }

        public ProcessMetrics Metrics;

        internal uint PPID;

        private static ManagementBaseObject _managementBaseObject;

        internal GFProcess(uint pid) { ProcessID = pid; }

        internal GFProcess(ManagementBaseObject managementBaseObject)
        {
            ProcessID = uint.Parse(managementBaseObject.Properties["ProcessID"].Value.ToString());

            try
            {
                PPID = uint.Parse(managementBaseObject.Properties["ParentProcessId"].Value.ToString());
            }
            catch (System.Exception) { }

            ProcessName = managementBaseObject.Properties["Name"].Value.ToString();

            if (!IsService)
            {
                ProcessDescription = managementBaseObject.Properties["description"].Value?.ToString();
                _executablePath = managementBaseObject.Properties["ExecutablePath"].Value?.ToString();
                CommandLine = managementBaseObject.Properties["CommandLine"].Value?.ToString();
            }
        }

        public static object GetUnlistedProperty(string propertyName)
        {
            try
            {
                if (_managementBaseObject.Properties[propertyName] != null)
                {
                    return _managementBaseObject.Properties[propertyName].Value;
                }
                else return null;
            }
            catch (System.Exception)
            {
                return "0";
            }
        }

        public static int GetSubProcessesCount(GFProcess root)
        {
            int count = root.SubProcesses.Count;
            for (int i = 0; i < root.SubProcesses.Count; i++)
            {
                count += GetSubProcessesCount(root.SubProcesses[i]);
            }
            return count;
        }

        private GFWindow GetMainWindow()
        {
            foreach (var window in Windows)
            {
                if (window.IsVisible && window.Title != null) return window;
            }
            return null;
        }

        private bool IsProcessVisible()
        {
            foreach (var window in Windows)
            {
                if (window.IsVisible) return true;
            }
            return false;
        }

        public override string ToString()
        {
            return ProcessName + " #" + ProcessID + " (" + GetSubProcessesCount(this) + ")";
        }
    }

    public class GFWindow
    {
        public readonly uint ProcessID;
        public readonly IntPtr Handle;

        public string Title => User32.GetWindowTitle(this);

        public bool IsVisible => User32.IsWindowVisible(this);

        public WindowRectangle Rectangle => User32.GetWindowRectangle(this);

        internal GFWindow(IntPtr handle, uint pid)
        {
            Handle = handle;
            ProcessID = pid;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public override string ToString()
            {
                return "Left : " + Left + " Top :" + Top + " Right : " + Right + " Bottom : " + Bottom;
            }
        }
    }

    public class ProcessMetrics
    {
        internal GFProcess GFProcess;

        public DateTime PreviousUpdate;
        public DateTime NewUpdate;

        public double CPUUsage
        {
            get
            {
                return ((CurrentMetrics["PercentProcessorTime"] - PreviousMetrics["PercentProcessorTime"]) / (ulong)Computer.Processors[0].NumberOfLogicalProcessors) / 100000.0;
            }
        }

        public double CPUUsageTree
        {
            get
            {
                double total = CPUUsage;

                foreach (var subP in GFProcess.SubProcesses)
                {
                    if (subP.Metrics != null)
                        total += subP.Metrics.CPUUsageTree;
                }

                return total;
            }
        }

        public double RAMWorkingSetKb
        {
            get { return CurrentMetrics["WorkingSetPrivate"] / 1024; }
        }

        public double RAMWorkingSetMb => RAMWorkingSetKb / 1024;

        public double RAMWorkingSetKbTree
        {
            get
            {
                double total = RAMWorkingSetKb;

                foreach (var subP in GFProcess.SubProcesses)
                {
                    if (subP.Metrics != null)
                        total += subP.Metrics.RAMWorkingSetKbTree;
                }

                return total;
            }
        }

        public double RAMWorkingSetMbTree
        {
            get
            {
                return RAMWorkingSetKbTree / 1024;
            }
        }

        public double IOWriteReadKb
        {
            get
            {
                double v = CurrentMetrics["IOReadBytesPersec"] - PreviousMetrics["IOReadBytesPersec"];
                v += CurrentMetrics["IOWriteBytesPersec"] - PreviousMetrics["IOWriteBytesPersec"];
                v += CurrentMetrics["IOOtherBytesPersec"] - PreviousMetrics["IOOtherBytesPersec"];
                
                return v / 1024;
            }
        }
        public double IOWriteReadMb => IOWriteReadKb / 1024;


        public double IOWriteReadKbTree
        {
            get
            {
                double total = IOWriteReadKb;

                foreach (var subP in GFProcess.SubProcesses)
                {
                    if (subP.Metrics != null)
                        total += subP.Metrics.IOWriteReadKbTree;
                }

                return total;
            }
        }
        public double IOWriteReadMbTree => IOWriteReadKbTree / 1024;


        public double NetworkUPDOWNKb
        {
            get
            {
                return (CurrentMetrics["IODataBytesPersec"] - PreviousMetrics["IODataBytesPersec"]) / 1024;
            }
        }
        public double NetworkUPDOWNMb => NetworkUPDOWNKb / 1024;

        public double NetworkUPDOWNKbTree
        {
            get
            {
                double total = NetworkUPDOWNKb;

                foreach (var subP in GFProcess.SubProcesses)
                {
                    if (subP.Metrics != null)
                        total += subP.Metrics.NetworkUPDOWNKbTree;
                }

                return total;
            }
        }
        public double NetworkUPDOWNMbTree => NetworkUPDOWNKbTree / 1024;

        internal Dictionary<string, ulong> PreviousMetrics;
        internal Dictionary<string, ulong> CurrentMetrics;

        internal IO_COUNTERS PreviousIOC; //SWAP IO_COUNTERS FOR WIN32_PROCESS SAME PROPERTIES. = FASTER !!! AND MAYBE ADMINS RIGHTS ?
        internal IO_COUNTERS CurrentIOC;  //SWAP IO_COUNTERS FOR WIN32_PROCESS SAME PROPERTIES. = FASTER !!! AND MAYBE ADMINS RIGHTS ?

        public ProcessMetrics(GFProcess gfProcess, Dictionary<string, ulong> newMetrics)
        {
            GFProcess = gfProcess;
            PreviousMetrics = newMetrics;
            CurrentMetrics = newMetrics;
            CurrentIOC = User32.GetProcessIOCounters(gfProcess);
        }

        public void Update(Dictionary<string, ulong> newMetrics)
        {
            PreviousMetrics = new Dictionary<string, ulong>();
            foreach (var item in CurrentMetrics)
            {
                PreviousMetrics.Add(item.Key, item.Value);
            }

            CurrentMetrics = newMetrics;
            PreviousIOC = CurrentIOC;
            CurrentIOC = User32.GetProcessIOCounters(GFProcess);
        }

        internal struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

    }
}