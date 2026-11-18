using System.Runtime.InteropServices;

namespace HS175D
{
    /// <summary>
    /// Minimal XInput wrapper for Xbox/XInput-compatible controllers - no NuGet dependency, just
    /// P/Invoke into xinput1_4.dll (present on Windows 8+, so fine on this machine). Only reads the
    /// two analog thumbsticks, which is all AirSimPanel needs to drive the same roll/pitch/yaw/
    /// throttle fields the on-screen virtual sticks already do.
    /// </summary>
    public static class XInputGamepad
    {
        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll")]
        static extern int XInputGetState(int dwUserIndex, out XINPUT_STATE pState);

        // Real analog sticks rarely rest at exactly (0,0) - without a deadzone the vehicle would
        // slowly drift even with hands off the sticks. Rescales the post-deadzone range back to a
        // full -1..1 span rather than leaving a dead gap right past the threshold.
        const float StickDeadzone = 0.15f;

        public readonly record struct State(float LeftX, float LeftY, float RightX, float RightY);

        /// <summary>Polls controller slot 0-3 (0 = first connected). Returns false if nothing's
        /// connected there, or if xinput1_4.dll itself isn't present (defensive - always present on
        /// Windows 8+, but this keeps the panel working with no controller plugged in either way).</summary>
        public static bool TryGetState(int index, out State state)
        {
            state = default;
            try
            {
                if (XInputGetState(index, out var raw) != 0) return false;   // non-zero = ERROR_DEVICE_NOT_CONNECTED (or similar)
                state = new State(
                    ApplyDeadzone(raw.Gamepad.sThumbLX / 32768f),
                    ApplyDeadzone(raw.Gamepad.sThumbLY / 32768f),
                    ApplyDeadzone(raw.Gamepad.sThumbRX / 32768f),
                    ApplyDeadzone(raw.Gamepad.sThumbRY / 32768f));
                return true;
            }
            catch (DllNotFoundException) { return false; }
        }

        static float ApplyDeadzone(float v)
        {
            float mag = Math.Abs(v);
            if (mag < StickDeadzone) return 0f;
            float rescaled = Math.Clamp((mag - StickDeadzone) / (1f - StickDeadzone), 0f, 1f);
            return Math.Sign(v) * rescaled;
        }
    }
}
