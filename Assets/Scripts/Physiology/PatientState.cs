using System;

namespace Floex.Physiology
{
    /// <summary>
    /// Pure-C# patient physiological state for the Floex VR simulator (Phase 3B, Week 4).
    ///
    /// DESIGN RULES (do not break):
    ///  - NO UnityEngine dependency. No MonoBehaviour. This compiles and unit-tests
    ///    outside Unity. A separate PatientStateDriver (MonoBehaviour) calls Tick().
    ///  - Week 4 scope: data container + time advance ONLY. No inter-variable
    ///    physiology (RPM->pressure->flow etc.) — that is Week 6 work, with KRB.
    ///    Tick() currently advances time-on-bypass and nothing else.
    ///  - All 12 variables carry documented units. Initial values are resting-adult
    ///    placeholders (clinical numbers — KRB's domain to validate later).
    /// </summary>
    public class PatientState
    {
        // --- The 12 physiological variables (roadmap Week 4) ---

        /// <summary>Heart rate (beats/min).</summary>
        public double HeartRate;                 // bpm

        /// <summary>Mean arterial blood pressure (mmHg).</summary>
        public double BloodPressure;             // mmHg (MAP)

        /// <summary>Mixed venous oxygen saturation (%).</summary>
        public double SvO2;                       // %

        /// <summary>Hematocrit — packed red cell volume fraction (%).</summary>
        public double Hematocrit;                 // %

        /// <summary>Core body temperature (degrees Celsius).</summary>
        public double Temperature;                // degC

        /// <summary>Arterial partial pressure of oxygen (mmHg).</summary>
        public double ArterialPO2;                // mmHg

        /// <summary>Arterial partial pressure of carbon dioxide (mmHg).</summary>
        public double ArterialPCO2;               // mmHg

        /// <summary>Pump (arterial) blood flow (L/min).</summary>
        public double PumpFlow;                   // L/min

        /// <summary>Sweep / fresh gas flow to the oxygenator (L/min).</summary>
        public double GasFlow;                     // L/min

        /// <summary>Fraction of inspired oxygen to the oxygenator (0..1).</summary>
        public double FiO2;                        // fraction 0..1

        /// <summary>Sweep gas ratio (gas flow : blood flow), dimensionless.</summary>
        public double SweepGas;                    // ratio

        /// <summary>Base excess (mEq/L) — metabolic acid-base offset.</summary>
        public double BaseExcess;                  // mEq/L

        // --- Derived / bookkeeping ---

        /// <summary>Elapsed time on cardiopulmonary bypass (seconds).</summary>
        public double TimeOnBypassSeconds;

        /// <summary>Whether the patient is currently on bypass (drives time accrual).</summary>
        public bool OnBypass;

        public PatientState()
        {
            ResetToRestingDefaults();
        }

        /// <summary>
        /// Resting-adult placeholder values. CLINICAL NUMBERS — placeholders only,
        /// to be reviewed with KRB before they mean anything physiologically.
        /// </summary>
        public void ResetToRestingDefaults()
        {
            HeartRate          = 70;    // bpm
            BloodPressure      = 80;    // mmHg MAP
            SvO2               = 75;    // %
            Hematocrit         = 40;    // %
            Temperature        = 37.0;  // degC
            ArterialPO2        = 100;   // mmHg
            ArterialPCO2       = 40;    // mmHg
            PumpFlow           = 0;     // L/min
            GasFlow            = 0;     // L/min
            FiO2               = 0.21;  // fraction
            SweepGas           = 0;     // ratio
            BaseExcess         = 0;     // mEq/L
            TimeOnBypassSeconds = 0;
            OnBypass            = false;
        }

        /// <summary>
        /// Advance the state by dt seconds. Week 4: advances time-on-bypass only
        /// (when OnBypass). NO physiology coupling yet — that arrives Week 6.
        /// dt is taken as a parameter (not read from Unity) so the class stays
        /// engine-independent and deterministically testable.
        /// </summary>
        public void Tick(double dtSeconds)
        {
            if (dtSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(dtSeconds), "dt must be >= 0");

            if (OnBypass)
                TimeOnBypassSeconds += dtSeconds;
        }

        /// <summary>Convenience: time on bypass as whole mm:ss for display.</summary>
        public string TimeOnBypassClock()
        {
            int total = (int)TimeOnBypassSeconds;
            int m = total / 60;
            int s = total % 60;
            return $"{m:D2}:{s:D2}";
        }
    }
}