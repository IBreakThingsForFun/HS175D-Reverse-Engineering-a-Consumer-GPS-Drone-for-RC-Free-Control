namespace HS175D
{
    /// <summary>Who is driving the sticks. Order matters only for human-vs-AI preemption.</summary>
    public enum StickSource { LocalUI, Gamepad, Browser, Ai }

    /// <summary>
    /// Decides who may write the sticks at any instant. The three human sources (local sliders, the Xbox
    /// pad, the browser) always write and become the owner; the AI driver yields whenever ANY human source
    /// has been active within the cooldown. This is the "human action preempts the AI" rule — kept simple
    /// (last human wins; the user won't fight two human surfaces at once) but centralized so every stick
    /// path goes through one gate.
    /// </summary>
    public static class ControlArbiter
    {
        static readonly object _lock = new();
        const long HumanCooldownMs = 1500;

        static long _lastHumanMs = -100000;
        static StickSource _owner = StickSource.Ai;

        public static StickSource Owner { get { lock (_lock) return _owner; } }
        public static bool HumanActive => Environment.TickCount64 - System.Threading.Volatile.Read(ref _lastHumanMs) < HumanCooldownMs;

        static bool IsHuman(StickSource s) => s != StickSource.Ai;

        /// <summary>Gate a stick write. Humans always pass (and take ownership); AI passes only when no human is recently active.</summary>
        public static bool TryWrite(StickSource src)
        {
            long now = Environment.TickCount64;
            lock (_lock)
            {
                if (IsHuman(src)) { _lastHumanMs = now; _owner = src; return true; }
                if (now - _lastHumanMs < HumanCooldownMs) return false;   // AI yields to recent human
                _owner = StickSource.Ai;
                return true;
            }
        }

        /// <summary>Mark human activity without a stick write (e.g. a local slider Scroll) so the AI yields.</summary>
        public static void Touch(StickSource src)
        {
            if (!IsHuman(src)) return;
            lock (_lock) { _lastHumanMs = Environment.TickCount64; _owner = src; }
        }

        public static object Status() => new { owner = Owner.ToString(), humanActive = HumanActive };
    }
}
