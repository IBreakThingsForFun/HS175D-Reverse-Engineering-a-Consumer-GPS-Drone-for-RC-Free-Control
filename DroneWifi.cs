using System.Diagnostics;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Automates the drone's SoftAP (hostapd on ap0) over adb — band (2.4/5 GHz), multi-station
    /// (ap_isolate off), and restore-to-stock — without hand-editing files.
    ///
    /// Mirrors the drone's own wifi_switch.sh model:
    ///   /tmp/hostapd.conf        = LIVE config (hostapd reads this)
    ///   /netPrivate/hostapd.conf = PERSISTENT override (boot copies it over /tmp); our 2.4G lives here
    ///   /etc/hostapd.conf        = STOCK 5 GHz (factory baseline / restore target)
    /// A boot self-heal (selfheal.sh) reverts to stock 5 GHz if hostapd fails within ~120 s = lockout net.
    ///
    /// Applying a change restarts hostapd, which DROPS all WiFi clients (incl. our adb link), so the
    /// restart is fired DETACHED (setsid) to survive the dropped connection. After an apply the caller's
    /// PC may need to rejoin the AP (only if the SSID/band actually changed).
    /// </summary>
    public static class DroneWifi
    {
        static string Adb => DroneConsole.ResolveAdb();

        // Minimal known-good 2.4 GHz config (ssid is regenerated on-device to match the band prefix + MAC).
        const string Conf24 =
            "interface=ap0\n" +
            "ssid=HolyStoneFPV_2.4G_x\n" +
            "ctrl_interface=/tmp/hostapd\n" +
            "ctrl_interface_group=0\n" +
            "channel=2\n" +
            "beacon_int=80\n" +
            "dtim_period=1\n" +
            "max_num_sta=8\n" +
            "hw_mode=g\n" +
            "ieee80211n=0\n" +
            "wme_enabled=1\n" +
            "ht_capab=[SHORT-GI-20]\n" +
            "ap_isolate=0\n";

        // Regenerate ssid like wifi_switch.sh: keep the first two _-delimited tokens, append the MAC tail.
        const string FixSsid =
            "MAC=$(cat /sys/class/net/ap0/address|sed 's/://g'); hw=${MAC:6}; " +
            "pfx=$(grep '^ssid' /tmp/hostapd.conf|cut -d= -f2|cut -d_ -f1,2); " +
            "sed -i \"s/ssid=.*$/ssid=${pfx}_${hw}/g\" /tmp/hostapd.conf; ";

        // Detached restart so it outlives the WiFi/adb drop it causes.
        const string Restart =
            "setsid sh -c 'sleep 1; killall hostapd 2>/dev/null; sleep 1; hostapd /tmp/hostapd.conf -B' >/dev/null 2>&1 &";

        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Replace("\r", "")));

        static void EnsureConnected(string ip) => RunRaw($"connect {ip}:5555", 8000);

        static string RunRaw(string args, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo(Adb, args)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                using var p = Process.Start(psi);
                if (p == null) return "(adb failed to start)";
                string o = p.StandardOutput.ReadToEnd(), e = p.StandardError.ReadToEnd();
                p.WaitForExit(timeoutMs);
                return (o + e).Trim();
            }
            catch (Exception ex) { return "(error) " + ex.Message; }
        }

        static string Shell(string ip, string cmd, int timeoutMs = 12000)
        {
            EnsureConnected(ip);
            // Single-arg shell string; inner quoting kept simple. cmd uses sh syntax.
            return RunRaw($"-s {ip}:5555 shell {cmd}", timeoutMs);
        }

        /// <summary>Read current band / isolation / ssid / hostapd state.</summary>
        public static string Status(string ip)
        {
            string r = Shell(ip,
                "\"echo SSID=$(grep ^ssid /tmp/hostapd.conf|cut -d= -f2); " +
                "echo HW_MODE=$(grep ^hw_mode /tmp/hostapd.conf|cut -d= -f2); " +
                "echo CHANNEL=$(grep ^channel /tmp/hostapd.conf|cut -d= -f2); " +
                "echo AP_ISOLATE=$(grep ^ap_isolate /tmp/hostapd.conf|cut -d= -f2); " +
                "echo MAX_STA=$(grep ^max_num_sta /tmp/hostapd.conf|cut -d= -f2); " +
                "echo PERSIST_2.4=$( [ -e /netPrivate/hostapd.conf ] && grep -q hw_mode=g /netPrivate/hostapd.conf && echo yes || echo no); " +
                "echo HOSTAPD=$(pidof hostapd >/dev/null && echo running || echo DOWN); " +
                "echo CLIENTS=$(cat /proc/net/arp 2>/dev/null|grep ap0|wc -l)\"");
            return r;
        }

        /// <summary>Switch to 2.4 GHz with multi-station (ap_isolate=0). persist => also write /netPrivate.</summary>
        public static string Set24G(string ip, bool persist)
        {
            string write = $"echo {B64(Conf24)} | base64 -d > /tmp/hostapd.conf; ";
            string persistCmd = persist ? "cp /tmp/hostapd.conf /netPrivate/hostapd.conf; " : "";
            return Shell(ip, $"\"{write}{FixSsid}{persistCmd}{Restart}\"");
        }

        /// <summary>Restore stock 5 GHz config (/etc). persist => remove the /netPrivate 2.4 override.</summary>
        public static string Set5G(string ip, bool persist)
        {
            string write = "cp /etc/hostapd.conf /tmp/hostapd.conf; ";
            string persistCmd = persist ? "rm -f /netPrivate/hostapd.conf; " : "";
            return Shell(ip, $"\"{write}{FixSsid}{persistCmd}{Restart}\"");
        }

        /// <summary>Force multi-station on the LIVE config (ap_isolate=0, max_num_sta=8) and restart.</summary>
        public static string EnsureMultiStation(string ip)
        {
            string cmd =
                "grep -q '^ap_isolate=' /tmp/hostapd.conf && sed -i 's/^ap_isolate=.*/ap_isolate=0/' /tmp/hostapd.conf || echo ap_isolate=0 >> /tmp/hostapd.conf; " +
                "grep -q '^max_num_sta=' /tmp/hostapd.conf && sed -i 's/^max_num_sta=.*/max_num_sta=8/' /tmp/hostapd.conf || echo max_num_sta=8 >> /tmp/hostapd.conf; " +
                Restart;
            return Shell(ip, $"\"{cmd}\"");
        }

        /// <summary>Factory AP: stock 5 GHz, drop the persistent override. The panic button.</summary>
        public static string RestoreFactory(string ip) => Set5G(ip, persist: true);
    }
}
