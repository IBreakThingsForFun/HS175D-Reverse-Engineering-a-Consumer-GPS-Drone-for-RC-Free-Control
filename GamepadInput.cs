using System.Runtime.InteropServices;

namespace HS175D
{
    /// <summary>
    /// Minimal XInput interop for Xbox controllers — no NuGet dependency. Polls player 1 via
    /// <c>XInputGetState</c> (xinput1_4.dll on Win8+, falling back to the 9_1_0 stub). All failures
    /// degrade to "not connected" so a missing DLL or unplugged pad never throws into the UI loop.
    /// </summary>
    public static class XInput
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Pad
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX, sThumbLY, sThumbRX, sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct State { public uint dwPacketNumber; public Pad Gamepad; }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")] static extern uint GetState14(uint index, out State state);
        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")] static extern uint GetState91(uint index, out State state);

        static int _which;   // 0 = untried, 1 = 1_4, 2 = 9_1_0, -1 = none available

        public static bool TryGetState(uint index, out State state)
        {
            state = default;
            try
            {
                if (_which == 0)
                {
                    try { uint r = GetState14(index, out state); _which = 1; return r == 0; }
                    catch (DllNotFoundException) { _which = 2; }
                    catch (EntryPointNotFoundException) { _which = 2; }
                }
                if (_which == 1) return GetState14(index, out state) == 0;
                if (_which == 2)
                {
                    try { return GetState91(index, out state) == 0; }
                    catch (DllNotFoundException) { _which = -1; }
                    catch (EntryPointNotFoundException) { _which = -1; }
                }
                return false;
            }
            catch { return false; }
        }

        // Button bit masks
        public const ushort DUp = 0x0001, DDown = 0x0002, DLeft = 0x0004, DRight = 0x0008,
                            Start = 0x0010, Back = 0x0020, LThumb = 0x0040, RThumb = 0x0080,
                            LB = 0x0100, RB = 0x0200, A = 0x1000, B = 0x2000, X = 0x4000, Y = 0x8000;
    }
}
