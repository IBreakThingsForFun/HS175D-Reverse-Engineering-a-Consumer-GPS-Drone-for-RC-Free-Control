using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Thin managed wrapper over the Windows Native WiFi API (wlanapi.dll) — pure P/Invoke, no
    /// external package. Drives a PC-side WiFi adapter (e.g. the Cisco Valet AM10 / Ralink RT3072):
    /// enumerate adapters, scan, list networks, connect (open or WPA2-PSK), disconnect, read live RSSI.
    ///
    /// This is the PC's OWN radio (station mode). It is unrelated to <see cref="DroneWifi"/>, which
    /// controls the drone's on-board SoftAP over adb. The intended use is dedicating a second adapter
    /// to the drone's AP so the built-in WiFi stays on the normal network.
    ///
    /// Monitor mode / packet injection are NOT exposed — Windows drivers don't provide them for this
    /// class of adapter (that needs Linux + rt2800usb). This is connect/scan/signal only.
    /// </summary>
    public sealed class WlanClient : IDisposable
    {
        IntPtr _handle;

        public WlanClient() => Check(W.WlanOpenHandle(2, IntPtr.Zero, out _, out _handle), "WlanOpenHandle");

        public void Dispose()
        {
            if (_handle != IntPtr.Zero) { W.WlanCloseHandle(_handle, IntPtr.Zero); _handle = IntPtr.Zero; }
        }

        // ---- adapters ----

        public List<WifiInterface> GetInterfaces()
        {
            var result = new List<WifiInterface>();
            Check(W.WlanEnumInterfaces(_handle, IntPtr.Zero, out IntPtr list), "WlanEnumInterfaces");
            try
            {
                int n = Marshal.ReadInt32(list);                        // dwNumberOfItems
                int header = 8;                                         // dwNumberOfItems + dwIndex
                int stride = Marshal.SizeOf<W.WLAN_INTERFACE_INFO>();
                for (int i = 0; i < n; i++)
                {
                    var info = Marshal.PtrToStructure<W.WLAN_INTERFACE_INFO>(list + header + i * stride);
                    result.Add(new WifiInterface(info.InterfaceGuid, info.strInterfaceDescription, W.StateName(info.isState)));
                }
            }
            finally { W.WlanFreeMemory(list); }
            return result;
        }

        // ---- scan + available networks ----

        /// <summary>Kick off an async scan. Results land in the driver's cache after a few seconds;
        /// call <see cref="GetNetworks"/> once they have (the form waits, then refreshes).</summary>
        public void Scan(Guid iface) => Check(W.WlanScan(_handle, ref iface, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero), "WlanScan");

        public List<WifiNetwork> GetNetworks(Guid iface)
        {
            var result = new List<WifiNetwork>();
            Check(W.WlanGetAvailableNetworkList(_handle, ref iface, 0, IntPtr.Zero, out IntPtr list), "WlanGetAvailableNetworkList");
            try
            {
                int n = Marshal.ReadInt32(list);
                int header = 8;
                int stride = Marshal.SizeOf<W.WLAN_AVAILABLE_NETWORK>();
                var seen = new HashSet<string>();
                for (int i = 0; i < n; i++)
                {
                    var net = Marshal.PtrToStructure<W.WLAN_AVAILABLE_NETWORK>(list + header + i * stride);
                    string ssid = W.SsidToString(net.dot11Ssid);
                    // Collapse the duplicate rows the API returns for the same SSID (per-profile + bare).
                    if (!seen.Add(ssid + "|" + net.dot11DefaultAuthAlgorithm)) continue;
                    result.Add(new WifiNetwork(
                        ssid, net.strProfileName, net.wlanSignalQuality, W.QualityToDbm(net.wlanSignalQuality),
                        net.bSecurityEnabled, W.AuthName(net.dot11DefaultAuthAlgorithm), net.bNetworkConnectable));
                }
            }
            finally { W.WlanFreeMemory(list); }
            result.Sort((a, b) => b.SignalQuality.CompareTo(a.SignalQuality));
            return result;
        }

        // ---- current link (for the live RSSI readout) ----

        public WifiLink GetCurrentLink(Guid iface)
        {
            uint ret = W.WlanQueryInterface(_handle, ref iface, W.WLAN_INTF_OPCODE.current_connection,
                                            IntPtr.Zero, out _, out IntPtr data, IntPtr.Zero);
            if (ret != 0 || data == IntPtr.Zero)
                return new WifiLink(false, "", 0, 0, "disconnected");   // not-connected is a normal, non-error state
            try
            {
                var attr = Marshal.PtrToStructure<W.WLAN_CONNECTION_ATTRIBUTES>(data);
                uint q = attr.wlanAssociationAttributes.wlanSignalQuality;
                return new WifiLink(
                    attr.isState == W.WLAN_INTERFACE_STATE.connected,
                    W.SsidToString(attr.wlanAssociationAttributes.dot11Ssid),
                    q, W.QualityToDbm(q), W.StateName(attr.isState));
            }
            finally { W.WlanFreeMemory(data); }
        }

        // ---- connect / disconnect ----

        /// <summary>Connect the adapter to <paramref name="ssid"/>. A temporary all-user profile is
        /// written first: WPA2-PSK/AES when a password is given, open otherwise. Returns a reason code
        /// (0 = accepted; the association itself completes asynchronously).</summary>
        public uint Connect(Guid iface, string ssid, string? password)
        {
            string xml = string.IsNullOrEmpty(password) ? OpenProfileXml(ssid) : Wpa2PskProfileXml(ssid, password!);
            Check(W.WlanSetProfile(_handle, ref iface, 0, xml, null, true, IntPtr.Zero, out uint reason), "WlanSetProfile", reason);

            var p = new W.WLAN_CONNECTION_PARAMETERS
            {
                wlanConnectionMode = W.WLAN_CONNECTION_MODE.profile,
                strProfile = ssid,                     // profile name == SSID (we just wrote it)
                pDot11Ssid = IntPtr.Zero,
                pDesiredBssidList = IntPtr.Zero,
                dot11BssType = W.DOT11_BSS_TYPE.infrastructure,
                dwFlags = 0
            };
            Check(W.WlanConnect(_handle, ref iface, ref p, IntPtr.Zero), "WlanConnect");
            return reason;
        }

        public void Disconnect(Guid iface) => Check(W.WlanDisconnect(_handle, ref iface, IntPtr.Zero), "WlanDisconnect");

        // ---- profile XML (strings only) ----

        static string Esc(string s) => SecurityElement.Escape(s) ?? s;

        static string OpenProfileXml(string ssid) =>
            $"""
            <?xml version="1.0"?>
            <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
              <name>{Esc(ssid)}</name>
              <SSIDConfig><SSID><name>{Esc(ssid)}</name></SSID></SSIDConfig>
              <connectionType>ESS</connectionType>
              <connectionMode>manual</connectionMode>
              <MSM><security>
                <authEncryption><authentication>open</authentication><encryption>none</encryption><useOneX>false</useOneX></authEncryption>
              </security></MSM>
            </WLANProfile>
            """;

        static string Wpa2PskProfileXml(string ssid, string psk) =>
            $"""
            <?xml version="1.0"?>
            <WLANProfile xmlns="http://www.microsoft.com/networking/WLAN/profile/v1">
              <name>{Esc(ssid)}</name>
              <SSIDConfig><SSID><name>{Esc(ssid)}</name></SSID></SSIDConfig>
              <connectionType>ESS</connectionType>
              <connectionMode>manual</connectionMode>
              <MSM><security>
                <authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption>
                <sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{Esc(psk)}</keyMaterial></sharedKey>
              </security></MSM>
            </WLANProfile>
            """;

        static void Check(uint ret, string call, uint reason = 0)
        {
            if (ret == 0) return;
            string msg = $"{call} failed (0x{ret:X8})";
            if (reason != 0) msg += $", reason code {reason}";
            throw new Win32ExceptionLite(msg);
        }
    }

    // ---- managed result types (public API surface) ----

    public sealed record WifiInterface(Guid Id, string Description, string State);

    public sealed record WifiNetwork(
        string Ssid, string ProfileName, uint SignalQuality, int RssiDbm,
        bool SecurityEnabled, string Auth, bool Connectable);

    public sealed record WifiLink(bool Connected, string Ssid, uint SignalQuality, int RssiDbm, string State);

    /// <summary>Lightweight typed error so callers can surface a clean message without pulling in the
    /// Win32Exception message table for HRESULT-style wlan codes.</summary>
    public sealed class Win32ExceptionLite(string message) : Exception(message);

    /// <summary>All wlanapi.dll interop — P/Invoke, native structs/enums, and the conversion helpers
    /// that touch them. File-local so the native types never leak into the public surface.</summary>
    file static class W
    {
        public static int QualityToDbm(uint quality) => (int)(quality / 2.0) - 100;   // MSDN: 0%→-100dBm, 100%→-50dBm

        public static string SsidToString(DOT11_SSID s)
            => s.uSSIDLength == 0 ? "(hidden)" : Encoding.UTF8.GetString(s.ucSSID, 0, (int)Math.Min(s.uSSIDLength, 32u));

        public static string StateName(WLAN_INTERFACE_STATE s) => s switch
        {
            WLAN_INTERFACE_STATE.connected => "connected",
            WLAN_INTERFACE_STATE.disconnected => "disconnected",
            WLAN_INTERFACE_STATE.disconnecting => "disconnecting",
            WLAN_INTERFACE_STATE.associating => "associating",
            WLAN_INTERFACE_STATE.discovering => "discovering",
            WLAN_INTERFACE_STATE.authenticating => "authenticating",
            WLAN_INTERFACE_STATE.ad_hoc_network_formed => "ad-hoc",
            _ => "not ready"
        };

        public static string AuthName(DOT11_AUTH_ALGORITHM a) => a switch
        {
            DOT11_AUTH_ALGORITHM.Open => "Open",
            DOT11_AUTH_ALGORITHM.SharedKey => "WEP",
            DOT11_AUTH_ALGORITHM.WPA => "WPA-Ent",
            DOT11_AUTH_ALGORITHM.WPA_PSK => "WPA-PSK",
            DOT11_AUTH_ALGORITHM.RSNA => "WPA2-Ent",
            DOT11_AUTH_ALGORITHM.RSNA_PSK => "WPA2-PSK",
            DOT11_AUTH_ALGORITHM.WPA3 => "WPA3",
            DOT11_AUTH_ALGORITHM.WPA3_SAE => "WPA3-SAE",
            _ => "?"
        };

        [DllImport("wlanapi.dll")]
        public static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern void WlanFreeMemory(IntPtr pMemory);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanScan(IntPtr hClientHandle, ref Guid pInterfaceGuid, IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanGetAvailableNetworkList(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, WLAN_INTF_OPCODE OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);

        [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
        public static extern uint WlanSetProfile(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, string strProfileXml, string? strAllUserProfileSecurity, [MarshalAs(UnmanagedType.Bool)] bool bOverwrite, IntPtr pReserved, out uint pdwReasonCode);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanConnect(IntPtr hClientHandle, ref Guid pInterfaceGuid, ref WLAN_CONNECTION_PARAMETERS pConnectionParameters, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        public static extern uint WlanDisconnect(IntPtr hClientHandle, ref Guid pInterfaceGuid, IntPtr pReserved);

        public enum WLAN_INTERFACE_STATE : uint
        { not_ready = 0, connected, ad_hoc_network_formed, disconnecting, disconnected, associating, discovering, authenticating }

        public enum DOT11_BSS_TYPE : uint { infrastructure = 1, independent = 2, any = 3 }

        public enum WLAN_CONNECTION_MODE : uint { profile = 0, temporary_profile, discovery_secure, discovery_unsecure, auto, invalid }

        public enum WLAN_INTF_OPCODE : uint
        {
            autoconf_start = 0x000000000, autoconf_enabled, background_scan_enabled, media_streaming_mode,
            radio_state, bss_type, interface_state, current_connection, channel_number
        }

        public enum DOT11_AUTH_ALGORITHM : uint
        {
            Open = 1, SharedKey = 2, WPA = 3, WPA_PSK = 4, WPA_None = 5,
            RSNA = 6, RSNA_PSK = 7, WPA3 = 8, WPA3_SAE = 9, WPA3_OWE = 12
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strInterfaceDescription;
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] ucSSID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_AVAILABLE_NETWORK
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strProfileName;
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            public uint uNumberOfBssids;
            [MarshalAs(UnmanagedType.Bool)] public bool bNetworkConnectable;
            public uint wlanNotConnectableReason;
            public uint uNumberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)] public uint[] dot11PhyTypes;
            [MarshalAs(UnmanagedType.Bool)] public bool bMorePhyTypes;
            public uint wlanSignalQuality;
            [MarshalAs(UnmanagedType.Bool)] public bool bSecurityEnabled;
            public DOT11_AUTH_ALGORITHM dot11DefaultAuthAlgorithm;
            public uint dot11DefaultCipherAlgorithm;
            public uint dwFlags;
            public uint dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_ASSOCIATION_ATTRIBUTES
        {
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public byte[] dot11Bssid;
            public uint dot11PhyType;
            public uint uDot11PhyIndex;
            public uint wlanSignalQuality;
            public uint ulRxRate;
            public uint ulTxRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WLAN_SECURITY_ATTRIBUTES
        {
            [MarshalAs(UnmanagedType.Bool)] public bool bSecurityEnabled;
            [MarshalAs(UnmanagedType.Bool)] public bool bOneXEnabled;
            public DOT11_AUTH_ALGORITHM dot11AuthAlgorithm;
            public uint dot11CipherAlgorithm;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_CONNECTION_ATTRIBUTES
        {
            public WLAN_INTERFACE_STATE isState;
            public WLAN_CONNECTION_MODE wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strProfileName;
            public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
            public WLAN_SECURITY_ATTRIBUTES wlanSecurityAttributes;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WLAN_CONNECTION_PARAMETERS
        {
            public WLAN_CONNECTION_MODE wlanConnectionMode;
            [MarshalAs(UnmanagedType.LPWStr)] public string strProfile;
            public IntPtr pDot11Ssid;
            public IntPtr pDesiredBssidList;
            public DOT11_BSS_TYPE dot11BssType;
            public uint dwFlags;
        }
    }
}
