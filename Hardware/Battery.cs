using System;

namespace GrassField
{
    public class Battery
    {
        /// <summary>
        /// From 0 to 100
        /// </summary>
        public static string[] Availabilities = new string[]
        {
            "Other", "Unknown", "Running/Full Power", "Warning", "In Test", "Not Applicable", "Power Off", "Off Line", "Off Duty", "Degraded", "Not Installed",
            "Install Error", "Power Save - Unkown", "Power Save - Low Power Mode", "Power Save - Standby", "Power Cycle", "Power Save - Warning", "Paused",
            "Not Ready", "Not Configured", "Quiesced", "BatteryRechargeTime"
        };

        public string Availability { get; internal set; }
        public byte EstimatedChargeRemaining { get; internal set; }

        public event EventHandler AvailabilityChanged;

        public void Update()
        {
            var battery = WMI.ListAnyWin32("Win32_Battery", false)[0];
            EstimatedChargeRemaining = byte.Parse(battery["EstimatedChargeRemaining"].ToString());
            var newAvailability = Availabilities[Computer.Bounds(int.Parse(battery["Availability"].ToString()), 0, Battery.Availabilities.Length)];
            if (newAvailability != Availability)
            {
                Availability = newAvailability;
                AvailabilityChanged?.Invoke(this, null);
            }
        }
    }
}
