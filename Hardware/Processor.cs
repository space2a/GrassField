namespace GrassField
{
    public class Processor
    {
        public string Name { get; internal set; }
        public ushort MaxClockSpeed { get; internal set; }
        public ushort NumberOfCores { get; internal set; }
        public ushort NumberOfEnabledCore { get; internal set; }
        public ushort NumberOfLogicalProcessors { get; internal set; }

        public bool Virtualization { get; internal set; }

        public uint[] CachesKb = new uint[3];

        static Processor()
        {
            //Core... with Win32_PerfFormattedData_PerfOS_Processor... / Win32_PerfFormattedData_Counters_ProcessorInformation 

        }
    }
}
