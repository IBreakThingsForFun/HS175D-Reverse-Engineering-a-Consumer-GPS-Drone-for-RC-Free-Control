namespace HS175D
{
    /// <summary>
    /// A standard clamped PID controller for an OUTER control loop - it sits on top of whatever inner
    /// stabilization the flight controller underneath already does (AirSim's SimpleFlight in sim, the
    /// real HS175D's own onboard controller on hardware), turning a measured error into a -1..1 stick
    /// fraction the same way DriveOrbit/DriveLinePass already write _throttle. It doesn't replace inner-
    /// loop attitude/rate control and was never meant to - see AirSimPanel's AltitudeTuneLoop for how the
    /// gains below get measured, and why this is the right layer for this to operate at.
    ///
    /// Derivative-on-measurement (not on error) so a setpoint change doesn't produce a derivative
    /// "kick" - the classic PID gotcha where a step-changed target makes Kd spike from the instantaneous
    /// jump in error rather than reacting to the process's actual rate of change.
    /// </summary>
    public sealed class PidController
    {
        public double Kp, Ki, Kd;
        public double OutputMin = -1.0, OutputMax = 1.0;
        // Bounds the integral term's own contribution to the output range, independent of Ki - stops
        // integral windup (the accumulated error ballooning while the output is already saturated,
        // which would otherwise cause a large overshoot once the error finally crosses zero).
        public double IntegralMin = -1.0, IntegralMax = 1.0;

        double _integral;
        double _lastMeasurement;
        bool _hasLast;

        public PidController(double kp, double ki, double kd)
        {
            Kp = kp; Ki = ki; Kd = kd;
        }

        /// <param name="error">setpoint - measurement, in whatever unit the caller is controlling (e.g. metres of altitude error).</param>
        /// <param name="measurement">the raw process variable (e.g. current altitude) - used only for the derivative-on-measurement term.</param>
        /// <param name="dt">seconds since the last Update call.</param>
        public double Update(double error, double measurement, double dt)
        {
            if (dt <= 0) return 0.0;

            _integral = System.Math.Clamp(_integral + error * dt, IntegralMin, IntegralMax);

            double derivative = _hasLast ? -(measurement - _lastMeasurement) / dt : 0.0;
            _lastMeasurement = measurement;
            _hasLast = true;

            double output = Kp * error + Ki * _integral + Kd * derivative;
            return System.Math.Clamp(output, OutputMin, OutputMax);
        }

        /// <summary>Clears accumulated state (integral term, last-measurement history) - call this
        /// whenever a controller is handed a fresh setpoint after being idle, so stale history from a
        /// previous run/target doesn't leak into the new one.</summary>
        public void Reset()
        {
            _integral = 0.0;
            _hasLast = false;
        }

        /// <summary>Tuning for an INTEGRATING process with dead time - the correct model for this
        /// system, not the more commonly-cited first-order-lag reaction curve. Altitude here is the
        /// integral of a commanded velocity setpoint (throttle -> SimpleFlight velocity-hold -> altitude
        /// accumulates it over time), so a fixed throttle step doesn't settle to a new final ALTITUDE the
        /// way a self-regulating process would - it climbs at a roughly constant RATE for as long as the
        /// step is held. That rate-per-step-size is <paramref name="velocityGainMpsPerUnit"/> (measured
        /// m/s of altitude change per unit of commanded throttle, from the linear portion of a real
        /// open-loop step test - see AirSimPanel's AltitudeTuneLoop for how it's actually measured), and
        /// <paramref name="deadTimeSec"/> is the measured delay before that rate becomes established.
        ///
        /// Deliberately conservative (not an aggressive stability-margin formula): Kp is set well under
        /// the loop's theoretical critical gain for an integrator+dead-time loop (1/(velocityGain*L) is
        /// the marginal-stability point; this uses half that), integral action is slow (Ti = 6L) so it
        /// can't windup-oscillate, and Kd starts at a small fraction of Kp rather than a full derivative
        /// term, since a real installation's altitude reading carries some estimator noise a large Kd
        /// would amplify. These are meant to be a safe STARTING point to apply and flight-test, not a
        /// final answer - PidTuneForm reports them for review, it never auto-applies them.</summary>
        public static (double Kp, double Ki, double Kd) TuneFromIntegratingStepResponse(double velocityGainMpsPerUnit, double deadTimeSec)
        {
            double l = System.Math.Max(deadTimeSec, 0.1);   // floor - a near-zero dead time would blow up Kp
            double gain = System.Math.Abs(velocityGainMpsPerUnit) < 1e-6 ? 1e-6 : System.Math.Abs(velocityGainMpsPerUnit);
            double kp = 0.5 / (gain * l);
            double ti = 6.0 * l;
            double kd = kp * l * 0.25;
            return (kp, kp / ti, kd);
        }
    }
}
