using System;
using System.Collections.Generic;
using System.Management;

namespace GrassField
{
    public static class WMI
    {
        public static List<GFProcess> GetProcesses(List<GFProcess> existingProcesses = null, bool includeMetrics = true)
        {
            DateTime dateTime = DateTime.Now;

            //TODO : Conserver les processus deja existant et juste mettre à jour leur informations (metrics+...)
            SelectQuery selectQuery = new SelectQuery("SELECT * FROM Win32_Process");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(selectQuery);
            var data = searcher.Get();

            List<uint> services = GetServicesPID();
            //bool[] servicesOk = new bool[services.Count];

            List<GFProcess> processes = new List<GFProcess>();

            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS 1");

            foreach (var d in data)
            {
                uint pid = uint.Parse(d.Properties["ProcessID"].Value.ToString());
                int possibleServiceIndex = services.FindIndex(x => x == pid);

                if (possibleServiceIndex != -1)
                {
                    //processes.Add(services[possibleServiceIndex]);
                    //servicesOk[possibleServiceIndex] = true;
                    continue;
                }

                processes.Add(new GFProcess(d));
            }

            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS 2 COUNT : " + processes.Count);

            //for (int j = 0; j < services.Count; j++)
            //{
            //    if (!servicesOk[j])
            //    {
            //        processes.Add(services[j]);
            //    }
            //}


            if (includeMetrics)
            {
                SetMetrics(processes, existingProcesses);
            }

            User32.SetWindows(processes);

            Console.BackgroundColor = ConsoleColor.Magenta;
            Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS final");
            Console.ResetColor();
            return processes;
        }

        private static List<GFService> GetServices()
        {
            DateTime dateTime = DateTime.Now;

            SelectQuery selectQuery = new SelectQuery("SELECT * FROM Win32_Service");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(selectQuery);
            var data = searcher.Get();

            List<GFService> services = new List<GFService>();

            foreach (ManagementObject d in data)
            {
                services.Add(new GFService(d));
            }

            //Console.BackgroundColor = ConsoleColor.Green;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS final");
            //Console.ResetColor();
            return services;
        }

        private static List<uint> GetServicesPID()
        {
            DateTime dateTime = DateTime.Now;

            SelectQuery selectQuery = new SelectQuery("SELECT * FROM Win32_Service");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(selectQuery);
            var data = searcher.Get();

            List<uint> services = new List<uint>();

            foreach (ManagementObject d in data)
            {
                services.Add(uint.Parse(d.Properties["ProcessID"].Value.ToString()));
            }

            //Console.BackgroundColor = ConsoleColor.Green;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS final");
            //Console.ResetColor();
            return services;
        }

        private static List<GFProcess> SetMetrics(List<GFProcess> processes, List<GFProcess> existingProcesses = null)
        {
            DateTime dateTime = DateTime.Now;
            Console.WriteLine("Fetching metrics...");
            ManagementObjectSearcher searcher =
            new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfRawData_PerfProc_Process");
            //new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_PerfProc_Process");
            var data = searcher.Get();

            //Console.BackgroundColor = ConsoleColor.Red;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS 1");

            Dictionary<GFProcess, int> processNodesDICT = new Dictionary<GFProcess, int>();

            for (int i = 0; i < processes.Count; i++)
            {
                processNodesDICT.Add(processes[i], i);
            }

            Dictionary<GFProcess, int> existingProcessNodeDict = new Dictionary<GFProcess, int>();
            if (existingProcesses != null)
            {
                for (int i = 0; i < existingProcesses.Count; i++)
                {
                    existingProcessNodeDict.Add(existingProcesses[i], i);
                }
            }

            //Console.BackgroundColor = ConsoleColor.Red;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS 2");

            foreach (ManagementObject queryObj in data)
            {
                KeyValuePair<GFProcess, int> process = new KeyValuePair<GFProcess, int>();
                KeyValuePair<GFProcess, int> OldProcess = new KeyValuePair<GFProcess, int>();

                uint pid = uint.Parse(queryObj.Properties["IDProcess"].Value.ToString());
                foreach (var p in processNodesDICT)
                    if (p.Key.ProcessID == pid) { process = p; break; }

                foreach (var p in existingProcessNodeDict)
                    if (p.Key.ProcessID == pid) { OldProcess = p; break; }

                if (process.Key == null)
                    continue;

                Dictionary<string, ulong> Metrics = new Dictionary<string, ulong>();

                foreach (var prop in queryObj.Properties)
                {
                    if (prop.Name == "Caption" || prop.Name == "Description" || prop.Name == "Name" || prop.Name == "IDProcess") continue;
                    ulong val = ulong.MaxValue;
                    if (prop.Value != null) val = ulong.Parse(prop.Value.ToString());
                    Metrics.Add(prop.Name, val);
                }
                //At this point the metrics are built

                if (OldProcess.Key == null || OldProcess.Key.Metrics == null)
                {
                    processes[process.Value].Metrics = new ProcessMetrics(processes[process.Value], Metrics);
                }
                else
                {
                    OldProcess.Key.Metrics.GFProcess = processes[process.Value];
                    processes[process.Value].Metrics = OldProcess.Key.Metrics;
                    processes[process.Value].Metrics.Update(Metrics);
                }

                //processNodesDICT.Remove(process.Key);
            }

            //Console.BackgroundColor = ConsoleColor.Red;
            //Console.WriteLine((DateTime.Now - dateTime).TotalMilliseconds + "MS final");
            //Console.ResetColor();
            return processes;
        }

        internal static List<Dictionary<string, object>> GetProcessorInfo()
        {
            ManagementObjectSearcher searcher =
            new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            //new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_PerfProc_Process");
            var data = searcher.Get();

            List<Dictionary<string, object>> CPUs = new List<Dictionary<string, object>>();

            foreach (ManagementObject queryObj in data)
            {
                Dictionary<string, object> cpu = new Dictionary<string, object>();
                foreach (var prop in queryObj.Properties)
                {
                    cpu.Add(prop.Name, prop.Value);
                }
                CPUs.Add(cpu);
            }

            return CPUs;
        }

        internal static ushort GetProcessorLoad()
        {
            ManagementObjectSearcher searcher =
            new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            var data = searcher.Get();
            foreach (ManagementObject queryObj in data)
            {
                return ushort.Parse(queryObj.Properties["LoadPercentage"].Value.ToString());
            }
            return 0;
        }

        internal static uint GetFreePhysicalMemory()
        {
            ManagementObjectSearcher searcher =
            new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            var data = searcher.Get();
            foreach (ManagementObject queryObj in data)
            {
                return (uint)(long.Parse(queryObj.Properties["FreePhysicalMemory"].Value.ToString()));
            }
            return 0;
        }

        public static List<Dictionary<string, object>> ListAnyWin32(string class32, bool consoleWrite)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + class32);
            //new ManagementObjectSearcher("SELECT * FROM Win32_PerfRawData_PerfProc_Process");
            var data = searcher.Get();

            List<Dictionary<string, object>> dicts = new List<Dictionary<string, object>>();

            foreach (ManagementObject queryObj in data)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (var prop in queryObj.Properties)
                {
                    if(consoleWrite)
                        Console.WriteLine(prop.Name + " : " + prop.Value);
                    dict.Add(prop.Name, prop.Value);
                }
                dicts.Add(dict);
            }

            return dicts;
        }
    }
}