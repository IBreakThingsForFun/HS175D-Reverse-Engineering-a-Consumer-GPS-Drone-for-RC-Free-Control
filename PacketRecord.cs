namespace HS175D
{
    /// <summary>One observed/recorded frame (used by the RE Workbench monitor/replay).</summary>
    sealed class PacketRecord
    {
        public long Ms;            // Environment.TickCount64 at observation
        public PacketDir Dir;
        public string Proto = "";  // "UDP19798" | "UDP23333" | "TCP4646" | "INJECT"
        public byte[] Data = System.Array.Empty<byte>();
    }
}
