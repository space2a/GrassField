namespace GrassField
{
    public class MemoryStick
    {
        public static string[] FormFactors = new string[]
        {
            "Unknown", "Other", "SIP", "DIP", "ZIP", "SOJ", "Proprietary", "SIMM", "DIMM", "TSOP",
            "PGA", "RIMM", "SODIMM", "SRIMM", "SMD", "SSMP", "QFP", "TQFP", "SOIC", "LCC", "PLCC",
            "BGA", "FPBGA", "LGA", "FB-DIMM"
        };

        public static string[] MemoryTypes = new string[]
        {
            "Unknown", "Other", "DRAM", "Synchronous DRAM", "Cache DRAM", "EDO", "EDRAM", "VRAM",
            "SRAM", "RAM", "ROM", "Flash", "EEPROM", "FEPROM", "EPROM", "CDRAM", "3DRAM", "SDRAM",
            "SGRAM", "RDRAM", "DDR", "DDR2", "DDR2 FB-DIMM", "Unknown2", "DDR3", "FBD2", "DDR4", "DDR5"
        };

        public uint CapacityKb { get; internal set; }
        public uint CapacityMb { get { return CapacityKb / 1024; } }

        public ushort ClockSpeed { get; internal set; }
        public ushort Voltage { get; internal set; }

        public string BankLabel { get; internal set; }
        
        public string FormFactor { get; internal set; }
        public string MemoryType { get; internal set; }

        public string Manufacturer { get; internal set; }

    }
}
