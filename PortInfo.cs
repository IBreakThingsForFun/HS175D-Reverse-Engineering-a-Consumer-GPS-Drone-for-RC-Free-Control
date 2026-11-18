using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;

namespace HS175D
{
    /// <summary>
    /// COM ports plus their Windows PnP friendly name (e.g. "Standard Serial over Bluetooth link"
    /// vs "Standard Serial over Bluetooth link" — the two ports Windows creates per paired Classic
    /// Bluetooth SPP device are otherwise indistinguishable by port name alone).
    /// </summary>
    public static class PortInfo
    {
        public sealed record Entry(string Port, string Description)
        {
            public override string ToString() => Description.Length > 0 ? $"{Port} — {Description}" : Port;
        }

        public static Entry[] List()
        {
            var names = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            var desc = new Dictionary<string, string>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
                foreach (ManagementObject o in searcher.Get())
                {
                    if (o["Name"] is not string name) continue;
                    var m = Regex.Match(name, @"\((COM\d+)\)$");
                    if (!m.Success) continue;
                    desc[m.Groups[1].Value] = name[..m.Index].Trim();
                }
            }
            catch { /* WMI unavailable — fall back to bare port names */ }

            return names.Select(p => new Entry(p, desc.TryGetValue(p, out var d) ? d : "")).ToArray();
        }
    }
}
