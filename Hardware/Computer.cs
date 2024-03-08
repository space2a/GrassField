using System.Collections.Generic;
using System.Threading;

namespace GrassField
{
    public static class Computer
    {
        //Computer
        public static string WindowCaption { get; }
        public static string ComputerName { get; }
        public static string RegisteredUser { get; }

        public static ushort NumberOfProcesses { get; }
        public static ushort NumberOfUsers { get; }

        public static bool Is64Bits { get; }

        public static ushort OSLanguage { get; }

        //Metrics

        public static ushort TotalCPUUsage => WMI.GetProcessorLoad();
        public static uint FreePhysicalMemoryKb => WMI.GetFreePhysicalMemory();

        //Hardware
        private static List<Processor> _processors;
        public static List<Processor> Processors
        {
            get
            {
                GetCPUs();
                return _processors;
            }
        }

        private static List<MemoryStick> _memorySticks;
        /// <summary>
        /// If one of the MemoryStick is null it means the physical motherboard ram slot is empty
        /// </summary>
        public static List<MemoryStick> MemorySticks
        {
            get
            {
                GetMemorySticks();
                return _memorySticks;
            }
        }
        public static uint MemoryCapacityKb 
        {
            get
            {
                uint total = 0;
                foreach (var memStick in MemorySticks)
                {
                    if (memStick == null) continue;
                    total += memStick.CapacityKb;
                }
                return total;
            }
        }
        public static uint MemoryCapacityMb { get { return MemoryCapacityKb / 1024; } }

        /// <summary>
        /// This value is null if the computer do not have a battery
        /// </summary>
        public static Battery Battery;

        static Computer()
        {
            var comp = WMI.ListAnyWin32("Win32_OperatingSystem", false)[0];

            WindowCaption = comp["Caption"].ToString();
            ComputerName = comp["CSName"].ToString();
            RegisteredUser = comp["RegisteredUser"].ToString();

            NumberOfProcesses = ushort.Parse(comp["NumberOfProcesses"].ToString());
            NumberOfUsers = ushort.Parse(comp["NumberOfUsers"].ToString());

            Is64Bits = comp["OSArchitecture"].ToString().IndexOf("64") != -1;

            OSLanguage = ushort.Parse(comp["OSLanguage"].ToString());

            GetBattery();
        }

        private static void GetCPUs()
        {
            if (_processors != null) return;
            _processors = new List<Processor>();
            var cpus = WMI.GetProcessorInfo();

            foreach (var mCPU in cpus)
            {
                Processor processor = new Processor();
                processor.Name = mCPU["Name"].ToString();
                processor.MaxClockSpeed = ushort.Parse(mCPU["MaxClockSpeed"].ToString());
                processor.NumberOfCores = ushort.Parse(mCPU["NumberOfCores"].ToString());
                processor.NumberOfEnabledCore = ushort.Parse(mCPU["NumberOfEnabledCore"].ToString());
                processor.NumberOfLogicalProcessors = ushort.Parse(mCPU["NumberOfLogicalProcessors"].ToString());

                //Level 1 cache isn't retrieved anywhere here, we are just speculating it, common CPU l1 cache is 64Kb per core
                processor.CachesKb[0] = (uint)((uint)64 * processor.NumberOfCores);
                processor.CachesKb[1] = uint.Parse(mCPU["L2CacheSize"].ToString());
                processor.CachesKb[2] = uint.Parse(mCPU["L3CacheSize"].ToString());

                processor.Virtualization = mCPU["VirtualizationFirmwareEnabled"].ToString() == "True";
                _processors.Add(processor);
            }
        }

        private static void GetMemorySticks()
        {
            if (_memorySticks != null) return;
            _memorySticks = new List<MemoryStick>();
            var memsticks = WMI.ListAnyWin32("Win32_PhysicalMemory", false);

            foreach (var memstick in memsticks)
            {
                if(memstick == null) { MemorySticks.Add(null); continue; }
                MemoryStick memoryStick = new MemoryStick();

                memoryStick.Manufacturer = memstick["Manufacturer"].ToString();
                memoryStick.BankLabel = memstick["BankLabel"].ToString();

                memoryStick.MemoryType = MemoryStick.MemoryTypes[Bounds(int.Parse(memstick["SMBIOSMemoryType"].ToString()), 0, MemoryStick.MemoryTypes.Length)];
                memoryStick.FormFactor = MemoryStick.FormFactors[Bounds(int.Parse(memstick["FormFactor"].ToString()), 0, MemoryStick.FormFactors.Length)];
                
                memoryStick.ClockSpeed = ushort.Parse(memstick["ConfiguredClockSpeed"].ToString());
                memoryStick.Voltage = ushort.Parse(memstick["ConfiguredVoltage"].ToString());

                memoryStick.CapacityKb = uint.Parse((long.Parse(memstick["Capacity"].ToString()) / 1024).ToString());

                _memorySticks.Add(memoryStick);
            }

        }

        private static void GetBattery()
        {
            var battery = WMI.ListAnyWin32("Win32_Battery", true)[0];
            if (int.Parse(battery["BatteryStatus"].ToString()) == 10) return; //No battery installed on this computer.

            Battery = new Battery();
            Battery.EstimatedChargeRemaining = byte.Parse(battery["EstimatedChargeRemaining"].ToString());
            Battery.Availability = Battery.Availabilities[Bounds(int.Parse(battery["Availability"].ToString()), 0, Battery.Availabilities.Length)];

        }

        public static void RefreshEvery(int ms)
        {
            new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(ms);
                    Battery.Update();
                }
            }).Start();
        }

        internal static int Bounds(int val, int min, int max)
        {
            if (val < min) return 0;
            else if (val > max) return 0;
            else return val;
        }
    }
}
